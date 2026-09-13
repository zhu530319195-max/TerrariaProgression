using System;
using System.Numerics;
using System.Reflection;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariaProgression.Core;
using TerrariaProgression.Players;
using TerrariaProgression.Talents;

namespace ProgressionHarness;

public sealed partial class RuntimeChecks
{
    // Test-only invocation of pinned native entry points; production uses On hooks.
    private static void Native(Player p, string method, params object[] args) =>
        typeof(Player).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(p,args);
    private void RunP2()
    {
        Reset(); Config.TalentPointsPerLevel = "5"; Config.OnChanged(); A.Award(1000 * Experience.Scale);
        Check(A.State.AvailableTalentPoints == 15, "native XP award uses configured five points per level");
        Config.TalentPointsPerLevel = "1000000000000000000000000000000"; Config.OnChanged();
        A.Award(Experience.Requirement(A.State.Level,50000)-A.State.CurrentExperience);
        Check(A.State.AvailableTalentPoints == BigInteger.Pow(10,30)+15, "runtime configured reward exceeds int without rounding");
        var tag = new TagCompound(); A.SaveData(tag); B.LoadData(tag);
        Check(B.State.TotalTalentPointsEarned == A.State.TotalTalentPointsEarned, "native save persists lifetime awarded points");
        Config.TalentPointsPerLevel = "0"; Config.OnChanged(); A.Award(Experience.Requirement(A.State.Level,50000));
        Check(A.State.Level == 6 && A.State.AvailableTalentPoints == BigInteger.Pow(10,30)+15, "zero reward does not retroactively remove points");
        Config.TalentPointsPerLevel = "-2"; Config.OnChanged(); Check(Config.PointsPerLevel == 1, "invalid negative config returns to default");
        Config.TalentPointsPerLevel = "1";
        Reset(); A.Award(100000000 * Experience.Scale);
        var p = A.Player; var functional = p.GetModPlayer<FunctionalTalentPlayer>();
        foreach (var t in FunctionalTalentRegistry.All)
            Check(A.ApplyTalent(TalentOperation.Upgrade,t.Definition.Id,TalentCategory.Utility,t.Definition.MaxLevel == 1 ? 1 : 3)==TalentResult.Success,"runtime functional purchase: "+t.Definition.Id);
        p.ResetEffects(); PlayerLoader.UpdateEquips(p);
        Check(p.noFallDmg && p.autoJump && p.noKnockback && p.waterWalk2 && p.lavaImmune && p.fireWalk,"native equip lifecycle applies purchased utility flags");
        Check(p.accWatch==3 && p.accCompass==1 && p.accWeatherRadio && p.accCalendar && p.accFishFinder && p.accThirdEye && p.accDreamCatcher && p.accCritterGuide && p.accOreFinder && p.findTreasure && p.accJarOfSouls,"information composite applies native readouts");
        A.ApplyTalent(TalentOperation.ToggleChild,"AllInformation",TalentCategory.Utility,3);
        p.ResetEffects(); PlayerLoader.UpdateEquips(p);
        Check(p.accCompass==0 && p.accWatch==3,"child compass toggle removes only its talent source");
        p.accCompass=1; functional.ApplyInformation(); Check(p.accCompass==1,"disabled child preserves real information accessory");
        var saved = new TagCompound(); A.SaveData(saved); B.LoadData(saved);
        Check(!FunctionalTalentRegistry.ChildEnabled(B.State,"AllInformation","Compass"),"native save persists per-information toggle");
        int waterX=Main.spawnTileX+30, waterY=Main.spawnTileY-10;
        p.position=new Vector2(waterX*16,waterY*16);
        for(int dx=-1;dx<4;dx++) for(int dy=-1;dy<5;dy++) {
            Main.tile[waterX+dx,waterY+dy].ClearEverything();
            var waterTile=Main.tile[waterX+dx,waterY+dy]; waterTile.LiquidType=LiquidID.Water; waterTile.LiquidAmount=255;
        }
        p.wet=true; p.breath=0; p.breathCD=0; p.statLife=100;
        Native(p,"CheckDrowning"); Check(p.statLife==100 && p.breath==0,"underwater ability stops drowning without refilling breath");
        A.ApplyTalent(TalentOperation.Disable,"UnderwaterBreathing",TalentCategory.Utility,1);
        Native(p,"CheckDrowning"); Check(p.breathCD>0 || p.statLife<100,"drowning resumes after disabling");
        var jump=ModContent.GetInstance<ProgressionExtraJump>();
        p.RefreshExtraJumps();
        for(int i=0;i<3;i++) { Check(p.AnyExtraJumpUsable(),"native purchased extra jump available "+i); ExtraJumpLoader.ProcessJumps(p); Check(functional.UsedJumps==i+1 && p.velocity.Y<0,"native extra jump starts and consumes one use"); p.StopExtraJumpInProgress(); }
        Check(!jump.CanStart(p),"three purchased jumps cannot start a fourth");
        A.ApplyTalent(TalentOperation.Disable,"MultiJump",TalentCategory.Utility,1);
        A.ApplyTalent(TalentOperation.Enable,"MultiJump",TalentCategory.Utility,1);
        Check(!jump.CanStart(p),"midair toggle does not reset used jumps");
        p.RefreshExtraJumps(); Check(jump.CanStart(p) && functional.UsedJumps==0,"native jump refresh restores uses");
        A.ApplyTalent(TalentOperation.DecreaseIntensity,"MultiJump",TalentCategory.Utility,2);
        ExtraJumpLoader.ProcessJumps(p); Check(!jump.CanStart(p),"active jump intensity reduces available count");
        A.ApplyTalent(TalentOperation.DisableEverything,"",TalentCategory.Utility,1);
        p.ResetEffects(); PlayerLoader.UpdateEquips(p);
        Check(!p.noFallDmg && !p.autoJump && !p.noKnockback && !p.waterWalk2 && !p.lavaImmune && !p.fireWalk,"disabling clears only talent contributions next native tick");
        p.noFallDmg=true; p.noKnockback=true; PlayerLoader.UpdateEquips(p);
        Check(p.noFallDmg && p.noKnockback,"disabled talents preserve equipment flags");
        Main.netMode=NetmodeID.Server; A.HasActionTick=false;
        SendTalentAction(A.SessionId,A.TalentRevision,TalentOperation.ToggleChild,"AllInformation",3);
        Check(!A.State.Talents["AllInformation"].DisabledEffects.Contains("Compass"),"server accepts bounded information child request");
        var retained=A.State; Main.netMode=NetmodeID.MultiplayerClient;
        Check(A.ApplyTalent(TalentOperation.ToggleChild,"AllInformation",TalentCategory.Utility,3)==TalentResult.NotReady && ReferenceEquals(retained,A.State),"client cannot mutate child state locally");
        Main.netMode=NetmodeID.Server; A.SessionReady=false; Receive(1,retained);
        Check(A.SessionReady && A.State.Talents.ContainsKey("AllInformation"),"protocol five imports functional talents");
        RunToolEfficiency();
    }
    private void RunToolEfficiency()
    {
        Reset(); A.Award(1000000*Experience.Scale); var p=A.Player;
        A.ApplyTalent(TalentOperation.Upgrade,"ToolSpeed",TalentCategory.Utility,10);
        int x=Main.spawnTileX+20,y=Main.spawnTileY-8;
        int Sample(int item,ushort tile,ushort wall,bool enabled) {
            A.ApplyTalent(enabled ? TalentOperation.Enable : TalentOperation.Disable,"ToolSpeed",TalentCategory.Utility,1);
            for(int dx=-3;dx<=3;dx++) for(int dy=-3;dy<=3;dy++) Main.tile[x+dx,y+dy].ClearEverything();
            if(tile>0) Main.tile[x,y].ResetToType(tile);
            Main.tile[x,y].WallType=wall;
            Main.tile[x,y+1].ResetToType(TileID.Grass);
            p.position=new Vector2(x*16-16,y*16-20); p.inventory[0]=new Item(item); p.selectedItem=0;
            p.ResetEffects(); p.controlUseItem=true; p.releaseUseItem=false; p.itemAnimation=30; p.itemTime=0; p.toolTime=0; p.poundRelease=true;
            Player.tileTargetX=x; Player.tileTargetY=y;
            Native(p,"ItemCheck_UseMiningTools",p.HeldItem);
            Check(ToolEfficiencySystem.MiningPlayer==null,"tool actor scope restored");
            return p.itemTime;
        }
        foreach(var sample in new[]{(ItemID.CopperPickaxe,TileID.Stone,(ushort)0), (ItemID.CopperAxe,TileID.Trees,(ushort)0), (ItemID.WoodenHammer,(ushort)0,WallID.Wood)}) {
            int normal=Sample(sample.Item1,sample.Item2,sample.Item3,false);
            int fast=Sample(sample.Item1,sample.Item2,sample.Item3,true);
            Check(normal>1 && fast>=1 && fast<normal,$"native tool interval item={sample.Item1}: {normal} -> {fast}");
        }
        p.inventory[0]=new Item(ItemID.WoodenSword); p.ApplyItemTime(p.HeldItem); int ordinary=p.itemTime;
        A.ApplyTalent(TalentOperation.Disable,"ToolSpeed",TalentCategory.Utility,1); p.ApplyItemTime(p.HeldItem);
        Check(p.itemTime==ordinary,"tool efficiency leaves ordinary attack interval unchanged");
        Sample(ItemID.CopperPickaxe,TileID.LihzahrdBrick,0,true);
        Check(Main.tile[x,y].HasTile && Main.tile[x,y].TileType==TileID.LihzahrdBrick,"tool efficiency cannot bypass pick power requirement");
        A.ApplyTalent(TalentOperation.DecreaseIntensity,"ToolSpeed",TalentCategory.Utility,5);
        Check(Math.Abs(ToolEfficiencySystem.Factor(p)-2)<.001,"tool efficiency adjustable to active level five");
        Reset();
    }
}
