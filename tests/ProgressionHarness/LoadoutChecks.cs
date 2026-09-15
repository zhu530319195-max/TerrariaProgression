using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariaProgression.Core;
using TerrariaProgression.Networking;
using TerrariaProgression.Players;
using TerrariaProgression.Talents;

namespace ProgressionHarness;
public sealed partial class RuntimeChecks
{
    private void RunLoadouts()
    {
        void Action(TalentOperation op,string value="",int count=1) => Check(A.ApplyTalent(op,value,TalentCategory.BaseStats,count)==TalentResult.Success,"loadout action "+op);
        try {
            Reset(); Config.GlobalTalentLevelLimit="-1";Config.TalentLevelOverrides.Clear();Config.OnChanged();
            A.Award(Experience.Cost(1,100,50000));
            Guid first=A.State.ActiveLoadoutId;
            Action(TalentOperation.RenameLoadout,"战斗");
            Action(TalentOperation.Upgrade,"MaxLife",50);Action(TalentOperation.Upgrade,"MaxMana",50);
            Action(TalentOperation.CreateLoadout,"采矿");
            Guid second=A.State.ActiveLoadoutId;
            Action(TalentOperation.Upgrade,"PickPower",100);
            var p=A.Player;var effects=p.GetModPlayer<NumericTalentPlayer>();
            p.statLife=50;p.statMana=5;p.potionDelay=900;p.manaRegenDelay=200;
            p.GetModPlayer<FunctionalTalentPlayer>().UsedJumps=3;
            p.GetModPlayer<GatheringPlayer>().BatchEnabled=true;
            p.GetModPlayer<GatheringPlayer>().ProtectionEnabled=true;
            p.ResetEffects();effects.ClampResources();
            Check(p.statLifeMax2==100 && p.statManaMax2==20 && ExtendedTalentPlayer.Level(p,"PickPower")==100,"native stats apply only mining allocation");
            var session=A.SessionId;
            for(int i=0;i<10;i++) {
                Action(TalentOperation.ActivateLoadout,(i%2==0?first:second).ToString("N"));
                p.ResetEffects();effects.ClampResources();
                Check(p.statLife==50 && p.statMana==5 && p.potionDelay==900 && p.manaRegenDelay==200,"switch does not heal, restore mana or reset resource timers "+i);
                Check(p.GetModPlayer<FunctionalTalentPlayer>().UsedJumps==3 && A.SessionId==session,"switch preserves jump usage and player session "+i);
            }
            Check(p.GetModPlayer<GatheringPlayer>().BatchEnabled && p.GetModPlayer<GatheringPlayer>().ProtectionEnabled,"Alt and K preference remains character-wide");
            Action(TalentOperation.ActivateLoadout,first.ToString("N"));p.ResetEffects();effects.ClampResources();
            Check(p.statLifeMax2==1350 && p.statManaMax2==1020 && ExtendedTalentPlayer.Level(p,"PickPower")==0,"native combat stats return without mining stacking");
            var tag=new TagCompound();A.SaveData(tag);B.LoadData(tag);
            Check(tag.GetInt("DataVersion")==4 && StateCodec.Encode(B.State).SequenceEqual(StateCodec.Encode(A.State)),"real player save restores both loadouts and current selection");
            // Exercise the actual outer v3 tag path with the old binary representation.
            using(var m=new MemoryStream()) {
                using(var w=new BinaryWriter(m,System.Text.Encoding.UTF8,true)) {
                    w.Write(3);foreach(var n in new[]{A.State.Level,A.State.CurrentExperience,A.State.TotalExperienceEarned,A.State.AvailableTalentPoints,A.State.TotalSpentTalentPoints,A.State.TotalTalentPointsEarned})StateCodec.WriteInteger(w,n);
                    w.Write(A.State.Talents.Count);foreach(var (id,t) in A.State.Talents) {
                        w.Write(id);w.Write(t.Enabled);w.Write(false);w.Write(0);w.Write(t.CostRuns.Count);
                        foreach(var run in t.CostRuns){StateCodec.WriteInteger(w,run.Cost);StateCodec.WriteInteger(w,run.Count);}
                    }
                }
                var old=new TagCompound{{"DataVersion",3},{"Progression",m.ToArray()}};B.LoadData(old);
                Check(B.State.Loadouts.Count==1 && B.State.Talents["MaxLife"].TalentLevel==50 && B.State.TotalTalentPointsEarned==100,"real v3 character migrates without erasing allocation");
            }
            Main.netMode=NetmodeID.Server;
            ulong revision=A.TalentRevision;
            A.HasActionTick=false;SendTalentAction(A.SessionId,revision,TalentOperation.CopyLoadout,"网络方案",1);
            Check(A.State.Loadouts.Count==3 && A.State.ActiveLoadout.Name=="网络方案","real server packet copies allocation");
            A.HasActionTick=false;SendTalentAction(A.SessionId,revision,TalentOperation.CopyLoadout,"重放",1);
            Check(A.State.Loadouts.Count==3,"replayed copy cannot multiply pages");
            revision=A.TalentRevision;
            A.HasActionTick=false;SendTalentAction(A.SessionId,revision,TalentOperation.ActivateLoadout,second.ToString("N"),1);
            Check(A.State.ActiveLoadoutId==second,"real server packet switches allocation");
            A.HasActionTick=false;SendTalentAction(A.SessionId,revision,TalentOperation.RefundEverything,"",1);
            Check(A.State.TotalSpentTalentPoints==100 && A.State.Talents.ContainsKey("PickPower"),"stale action from previous allocation cannot refund newly active build");
            byte[] before=StateCodec.Encode(A.State);
            A.HasActionTick=false;SendTalentAction(Guid.NewGuid(),A.TalentRevision,TalentOperation.DeleteLoadout,second.ToString("N"),1);
            Check(StateCodec.Encode(A.State).SequenceEqual(before),"wrong session cannot delete loadout");
            A.HasActionTick=false;SendTalentAction(A.SessionId,A.TalentRevision,TalentOperation.RenameLoadout,"[c/ffffff:invalid]",1);
            Check(StateCodec.Encode(A.State).SequenceEqual(before),"server validates names");
            p.dead=true;A.HasActionTick=false;SendTalentAction(A.SessionId,A.TalentRevision,TalentOperation.CreateLoadout,"死亡",1);p.dead=false;
            Check(StateCodec.Encode(A.State).SequenceEqual(before),"existing dead-player restriction covers loadout changes");
            var imported=A.State;A.SessionReady=false;Receive(1,imported);
            Check(A.SessionReady && A.State.Loadouts.Count==3 && A.State.ActiveLoadoutId==second,"join imports all allocations with selected one intact");
            Main.netMode=NetmodeID.MultiplayerClient;
            Check(A.ApplyTalent(TalentOperation.CreateLoadout,"本地伪造",TalentCategory.BaseStats,1)==TalentResult.NotReady,"client cannot create authoritative loadouts");
            Receive(2,imported,padding:true);
            Check(A.State.Loadouts.Count==3 && A.State.ActiveLoadoutId==second,"actual snapshot receive preserves complete loadout state");
            Main.netMode=NetmodeID.Server;Config.ShareExperienceServerWide=true;
            var npc=Spawn(100000);ServerStrike(npc,0,npc.life);Settle(npc);
            Check(A.State.Loadouts.All(page=>page.AvailablePoints+page.SpentPoints==A.State.TotalTalentPointsEarned)
                && A.State.TotalTalentPointsEarned>100,"shared experience increases every allocation allowance");
            // A pending real wall job must be cancelled before the next work frame.
            Reset();GatheringSystem.Clear();TreeReplantSystem.Clear();Config.EnableWorldGathering=Config.EnableAreaWallRemoval=true;
            A.Award(1000000*Experience.Scale);Action(TalentOperation.Upgrade,"AreaWallRemoval",10);Action(TalentOperation.Upgrade,"HammerPower",10);
            Action(TalentOperation.CopyLoadout,"另一拆墙方案");first=A.State.Loadouts[0].Id;
            int x=Main.spawnTileX+180,y=Main.spawnTileY-15;
            p=A.Player;p.Center=new Vector2(x*16,y*16);p.inventory[0]=new Item(ItemID.WoodenHammer);p.selectedItem=0;
            p.GetModPlayer<GatheringPlayer>().BatchEnabled=true;
            Config.MaxBlocksPerAction=1000;Config.GatheringWorkPerTick=1;Config.ProtectedTileAreas.Clear();
            for(int dx=-2;dx<=2;dx++)for(int dy=-2;dy<=2;dy++){var tile=Main.tile[x+dx,y+dy];tile.ClearEverything();tile.WallType=WallID.Wood;}
            Check(GatheringSystem.Request(p,GatheringMode.Wall,x,y,A.SessionId,A.TalentRevision,1,0,p.HeldItem.type)&&GatheringSystem.HasJob(0),"loadout cancellation fixture has real queued wall job");
            Action(TalentOperation.ActivateLoadout,first.ToString("N"));
            Check(!GatheringSystem.HasJob(0),"switch immediately cancels old job even if both builds own wall talent");
            GatheringSystem.ProcessJobs();Check(Main.tile[x+1,y].WallType==WallID.Wood,"cancelled loadout cannot remove remaining wall next frame");
            // Capture a real partial tree harvest and ensure another loadout cannot
            // consume that previous opportunity even when it also owns replanting.
            Action(TalentOperation.Upgrade,"AutoReplantTree");Config.EnableAutoReplantTree=true;
            Action(TalentOperation.CopyLoadout,"补种副本");
            p.inventory[0]=new Item(ItemID.PickaxeAxe);p.HeldItem.axe=100;p.inventory[1]=new Item(ItemID.Acorn,5);
            p.GetModPlayer<GatheringPlayer>().BatchEnabled=false;
            for(int dx=-8;dx<=8;dx++)for(int dy=-40;dy<=8;dy++)Main.tile[x+dx,y+dy].ClearEverything();
            for(int dx=-3;dx<=3;dx++) {Main.tile[x+dx,y+6].ResetToType(TileID.Stone);Main.tile[x+dx,y+5].ResetToType(TileID.Grass);}
            Check(WorldGen.GrowTree(x,y+5),"loadout cancellation tree fixture grows");
            var tool=typeof(Player).GetMethod("ItemCheck_UseMiningTools_ActuallyUseMiningTool",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
            var oldActor=EconomySystem.Actor;EconomySystem.Actor=p;
            try {tool.Invoke(p,new object[]{p.HeldItem,false,x,y+2});}
            finally {EconomySystem.Actor=oldActor;}
            Check(TreeReplantSystem.PendingCount==1,"manual tree harvest awaits remaining stump");
            Action(TalentOperation.ActivateLoadout,first.ToString("N"));
            Check(TreeReplantSystem.PendingCount==0 && p.inventory[1].stack==5,"switch cancels pending replant without consuming inventory");
        }
        finally {GatheringSystem.Clear();TreeReplantSystem.Clear();Reset();Config.OnChanged();}
    }
}
