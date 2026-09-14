using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TerrariaProgression.Core;
using TerrariaProgression.Players;
using TerrariaProgression.Networking;
using TerrariaProgression.Talents;

namespace ProgressionHarness;
public sealed partial class RuntimeChecks
{
    private void RunNativeBlasts()
    {
        int x=Main.spawnTileX+70,y=Main.spawnTileY-10;
        void Init() {
            Reset();GatheringSystem.Clear();A.Award(1000000000*Experience.Scale);
            Config.EnableWorldGathering=Config.EnableBlastRadius=true;Config.MaxBlastRadius=32;
            Config.MaxBlocksPerAction=1;Config.GatheringWorkPerTick=1;Config.ProtectedTileAreas.Clear();
            Main.hardMode=false;A.Player.Center=new Vector2((x-30)*16,y*16);
        }
        void Clear() {
            for(int dx=-35;dx<=35;dx++)for(int dy=-35;dy<=35;dy++)Main.tile[x+dx,y+dy].ClearEverything();
            foreach(var item in Main.item)item.active=false;Array.Clear(Main.timeItemSlotCannotBeReusedFor);
            foreach(var p in Main.projectile)p.active=false;
        }
        void Put(int dx,int dy,ushort type=TileID.Stone) {var t=Main.tile[x+dx,y+dy];t.ClearEverything();t.HasTile=true;t.TileType=type;}
        void Fill(bool wallHole,bool mixed=false) {
            Clear();
            for(int dx=-25;dx<=25;dx++)for(int dy=-25;dy<=25;dy++){Put(dx,dy);var t=Main.tile[x+dx,y+dy];t.WallType=WallID.Stone;}
            if(wallHole){var t=Main.tile[x+5,y];t.WallType=WallID.None;}
            if(mixed){Put(5,1,TileID.Sand);Put(5,2,TileID.Sand);var t=Main.tile[x-5,y];t.RedWire=true;t.HasActuator=true;Put(0,5,TileID.Meteorite);Put(0,-5,TileID.Hellstone);Put(-5,2,TileID.BlueDungeonBrick);}
        }
        List<(bool,ushort,ushort,bool,bool)> Snapshot() {
            var list=new List<(bool,ushort,ushort,bool,bool)>();
            for(int dx=-26;dx<=26;dx++)for(int dy=-26;dy<=26;dy++){var t=Main.tile[x+dx,y+dy];list.Add((t.HasTile,t.TileType,t.WallType,t.RedWire,t.HasActuator));}return list;
        }
        Projectile SpawnBomb(int type,int owner=0) {int n=Projectile.NewProjectile(new EntitySource_Misc("CI native terrain"),new Vector2(x*16+(BlastRadiusSystem.BaseRadius(type)==4?11:5),y*16+(BlastRadiusSystem.BaseRadius(type)==4?11:5)),Vector2.Zero,type,99,1,owner);return Main.projectile[n];}
        void Buy(int level=3)=>Check(A.ApplyTalent(TalentOperation.Upgrade,"BlastRadius",TalentCategory.Utility,level)==TalentResult.Success,"native blast purchase");
        void Reference(int type,int radius) {
            var p=new Projectile();p.SetDefaults(type);p.owner=0;var center=new Vector2(x*16,y*16);
            // No OnSpawn session: this is the unmodified native operation oracle.
            p.ExplodeTiles(center,radius,x-radius,x+radius,y-radius,y+radius,p.ShouldWallExplode(center,radius,x-radius,x+radius,y-radius,y+radius));
        }
        foreach(int type in new[]{ProjectileID.Bomb,ProjectileID.StickyBomb,ProjectileID.BouncyBomb,ProjectileID.Dynamite,ProjectileID.StickyDynamite,ProjectileID.BouncyDynamite}) {
            foreach(bool hole in new[]{false,true}) {
                Init();Buy();int radius=BlastRadiusSystem.BaseRadius(type)+3;
                Fill(hole);Reference(type,radius);var expected=Snapshot();
                Fill(hole);var bomb=SpawnBomb(type);bomb.PrepareBombToBlow();int damage=bomb.damage;bomb.Kill();
                Check(Snapshot().SequenceEqual(expected),"instant whole native terrain/wall result matches 2809 cells: "+type+" hole="+hole);
                Check(bomb.damage==damage&&bomb.GetGlobalProjectile<BlastRadiusProjectile>().Consumed,"one native explosion consumed, damage unchanged: "+type);
                Check(hole?Main.tile[x,y].WallType==WallID.None:Main.tile[x,y].WallType==WallID.Stone,"wall eligibility uses enlarged radius, not forced clear: "+type+" hole="+hole);
                for(int n=0;n<100;n++)GatheringSystem.ProcessJobs();Check(Snapshot().SequenceEqual(expected),"no deferred outer-ring work: "+type);
            }
        }
        Init();Buy();Fill(true,true);Reference(ProjectileID.Bomb,7);var mixedExpected=Snapshot();
        Fill(true,true);Config.ProtectedTileAreas.Add(new(){X=x-25,Y=y-25,Width=51,Height=51});var mixed=SpawnBomb(ProjectileID.Bomb);mixed.Kill();
        Check(Snapshot().SequenceEqual(mixedExpected),"sand, wired/actuated blocks and native unbreakable ores match vanilla despite gathering protection zone");
        Init();Buy(1000);Config.MaxBlastRadius=8;Fill(false);Reference(ProjectileID.Bomb,8);var capped=Snapshot();Fill(false);var large=SpawnBomb(ProjectileID.Bomb);large.Kill();
        Check(Snapshot().SequenceEqual(capped)&&A.State.Talents["BlastRadius"].TalentLevel==1000,"radius cap preserves complete native circle and purchased level; block quota ignored");
        Init();Buy();Fill(false);Reference(ProjectileID.Bomb,4);var baseline=Snapshot();Fill(false);Config.EnableBlastRadius=false;SpawnBomb(ProjectileID.Bomb).Kill();
        Check(Snapshot().SequenceEqual(baseline),"disabled permission preserves original native core");
        Init();Buy();Fill(false);A.ApplyTalent(TalentOperation.Disable,"BlastRadius",TalentCategory.Utility,1);SpawnBomb(ProjectileID.Bomb).Kill();
        Check(Snapshot().SequenceEqual(baseline),"disabled talent preserves original native core");
        Init();Buy();Fill(false);Reference(ProjectileID.Bomb,7);var serverExpected=Snapshot();Fill(false);var serverBomb=SpawnBomb(ProjectileID.Bomb);Main.netMode=NetmodeID.Server;Main.myPlayer=255;serverBomb.Kill();
        Check(Snapshot().SequenceEqual(serverExpected)&&serverBomb.GetGlobalProjectile<BlastRadiusProjectile>().Consumed,"actual dedicated-server projectile death performs full native blast");
        var after=Snapshot();serverBomb.GetGlobalProjectile<BlastRadiusProjectile>().OnKill(serverBomb,0);Check(Snapshot().SequenceEqual(after),"duplicate server OnKill cannot repeat destruction");
        Init();Buy();Fill(false);var clientBomb=SpawnBomb(ProjectileID.Bomb);var intact=Snapshot();Main.netMode=NetmodeID.MultiplayerClient;clientBomb.Kill();Check(Snapshot().SequenceEqual(intact),"owner client suppresses its native terrain copy for server-owned enhanced blast");
        Init();Buy();Clear();Put(5,0,TileID.Copper);A.ApplyTalent(TalentOperation.Upgrade,"MiningYield",TalentCategory.Economy,10);var lootBomb=SpawnBomb(ProjectileID.Bomb);Main.netMode=NetmodeID.Server;Main.myPlayer=255;lootBomb.Kill();
        Check(Main.item.Take(Main.maxItems).Where(i=>i.active&&i.type==ItemID.CopperOre).Sum(i=>i.stack)==2,"server native blast drops use actual thrower mining yield once");
        Init();Buy();Fill(false);var stale=SpawnBomb(ProjectileID.Bomb);stale.GetGlobalProjectile<BlastRadiusProjectile>().Session=Guid.NewGuid();Main.netMode=NetmodeID.Server;Main.myPlayer=255;stale.Kill();Check(Main.tile[x+5,y].HasTile,"stale projectile session cannot get server expansion");
        Init();Buy();foreach(int type in new[]{ProjectileID.Grenade,ProjectileID.BombFish,ProjectileID.ScarabBomb,ProjectileID.RocketI,ProjectileID.DirtBomb,ProjectileID.WetBomb})Check(BlastRadiusSystem.BaseRadius(type)==0,"unsupported explosive remains native: "+type);
        var trap=SpawnBomb(ProjectileID.Bomb);trap.trap=true;Check(!BlastRadiusSystem.Enabled(trap,A.SessionId),"trap cannot borrow player blast talent");trap.trap=false;trap.npcProj=true;Check(!BlastRadiusSystem.Enabled(trap,A.SessionId),"NPC projectile cannot borrow player blast talent");
        Config.MaxBlocksPerAction=1000;Config.GatheringWorkPerTick=32;Config.MaxBlastRadius=32;Config.ProtectedTileAreas.Clear();Main.netMode=NetmodeID.SinglePlayer;Main.myPlayer=0;
    }
    private void RunNativeMining()
    {
        int x=Main.spawnTileX+110,y=Main.spawnTileY-10;uint seq=0;
        void Advance()=>typeof(Main).GetField("_gameUpdateCount",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)!.SetValue(null,unchecked(Main.GameUpdateCount+100));
        void Init() {
            Reset();GatheringSystem.Clear();A.Award(1000000000*Experience.Scale);seq=0;
            Config.EnableWorldGathering=Config.EnableAreaMining=Config.EnableVeinMining=true;Config.MaxBlocksPerAction=1000;Config.GatheringWorkPerTick=32;Config.ProtectedTileAreas.Clear();
            A.Player.inventory[0]=new Item(ItemID.PickaxeAxe);A.Player.selectedItem=0;A.Player.Center=new Vector2(x*16,y*16);A.Player.pickSpeed=1;
            A.Player.GetModPlayer<GatheringPlayer>().BatchEnabled=true;
            for(int dx=-10;dx<=10;dx++)for(int dy=-10;dy<=10;dy++)Main.tile[x+dx,y+dy].ClearEverything();
            foreach(var item in Main.item)item.active=false;Array.Clear(Main.timeItemSlotCannotBeReusedFor);
            Check(A.ApplyTalent(TalentOperation.Upgrade,"AreaMining",TalentCategory.Utility,1)==TalentResult.Success,"native area mining purchase");
        }
        void Put(int dx,int dy,ushort type=TileID.Stone) {var t=Main.tile[x+dx,y+dy];t.ClearEverything();t.HasTile=true;t.TileType=type;}
        bool Has(int dx,int dy)=>Main.tile[x+dx,y+dy].HasTile;
        bool Request() {Advance();return GatheringSystem.Request(A.Player,GatheringMode.Area,x,y,A.SessionId,A.TalentRevision,++seq,0,A.Player.HeldItem.type);}
        void Drain() {for(int n=0;n<1000&&GatheringSystem.PendingCount>0;n++)GatheringSystem.ProcessJobs();Check(GatheringSystem.PendingCount==0,"native mining queue terminates");}
        Init();Check(!A.Player.GetModPlayer<GatheringPlayer>().ProtectionEnabled,"extra mining protection defaults off");
        Put(0,0);Put(1,0);var wired=Main.tile[x+1,y];wired.RedWire=true;wired.HasActuator=true;Put(-1,0,TileID.Sand);Put(-1,1);Config.ProtectedTileAreas.Add(new(){X=x+1,Y=y});
        Check(Request(),"default unprotected area begins");Drain();Check(!Has(1,0)&&!Has(-1,0),"native area removes wiring and sand without extra protection filters");
        Init();A.Player.GetModPlayer<GatheringPlayer>().ProtectionEnabled=true;Put(0,0);Put(1,0);wired=Main.tile[x+1,y];wired.RedWire=true;Put(-1,0,TileID.Sand);Put(-1,1);
        Check(Request(),"protected area begins");Drain();Check(Has(1,0)&&Has(-1,0),"optional protection retains wires and sand");
        foreach(bool protect in new[]{false,true}) {
            Init();A.Player.GetModPlayer<GatheringPlayer>().ProtectionEnabled=protect;Put(0,0);Put(1,-1);
            WorldGen.PlaceTile(x+1,y-2,TileID.Torches,mute:true,forced:true);
            Check(Has(1,-2),"native torch support fixture valid");Request();Drain();
            Check(Has(1,-1)==protect&&Has(1,-2)==protect,"furniture support and out-of-area native framing follow protection state: "+protect);
        }
        Init();A.Player.inventory[0]=new Item(ItemID.CopperPickaxe);Put(0,0);Put(1,0,TileID.Hellstone);for(int n=0;n<12&&Has(0,0);n++)Request();Drain();Check(Has(1,0),"protection off never bypasses native pick power gate");
        Init();Put(0,0);Put(1,0,TileID.Trees);Put(1,1,TileID.Grass);Put(-1,0,TileID.ImmatureHerbs);
        Check(!GatheringSystem.CanTouch(A.Player,new(x+1,y),GatheringMode.Area)&&!GatheringSystem.CanTouch(A.Player,new(x-1,y),GatheringMode.Area),"pick-only batch leaves tree and agriculture entry rules separate");
        Init();Put(0,0);Put(1,0);Check(Request()&&GatheringSystem.PendingCount==1,"native mining job pending before protection toggle");
        Check(GatheringSystem.SetProtection(A.Player,A.SessionId,++seq,true)&&GatheringSystem.PendingCount==0&&Has(1,0),"protection change stops in-progress mining");
        Check(!GatheringSystem.SetProtection(A.Player,A.SessionId,seq,false)&&A.Player.GetModPlayer<GatheringPlayer>().ProtectionEnabled,"replayed protection sequence cannot overwrite state");
        Check(!GatheringSystem.SetProtection(A.Player,Guid.NewGuid(),++seq,false)&&A.Player.GetModPlayer<GatheringPlayer>().ProtectionEnabled,"stale-session protection request rejected");
        A.Player.GetModPlayer<GatheringPlayer>().OnEnterWorld();Check(!A.Player.GetModPlayer<GatheringPlayer>().ProtectionEnabled,"new world resets protection off");
        Init();Main.netMode=NetmodeID.Server;Main.myPlayer=255;
        void Packet(Guid session,uint sequence,bool enabled) {using var m=new MemoryStream();using(var w=new BinaryWriter(m,System.Text.Encoding.UTF8,true)){w.Write(ProgressionNetwork.ProtocolVersion);w.Write((byte)7);w.Write(session.ToByteArray());w.Write(sequence);w.Write(enabled);}m.Position=0;ProgressionNetwork.Receive(new BinaryReader(m),0);}
        Packet(A.SessionId,1,true);Check(A.Player.GetModPlayer<GatheringPlayer>().ProtectionEnabled&&!B.Player.GetModPlayer<GatheringPlayer>().ProtectionEnabled,"server decodes protection intent for actual sender only");
        Packet(A.SessionId,1,false);Check(A.Player.GetModPlayer<GatheringPlayer>().ProtectionEnabled,"network replay protection holds");
        Packet(A.SessionId,2,false);Check(!A.Player.GetModPlayer<GatheringPlayer>().ProtectionEnabled,"server accepts newer protection-off intent");
        Config.EnableAreaMining=false;Put(0,0);Advance();Check(!GatheringSystem.Request(A.Player,GatheringMode.Area,x,y,A.SessionId,A.TalentRevision,3,0,A.Player.HeldItem.type)&&Has(0,0),"protection off cannot override server feature permission");
        Config.EnableAreaMining=true;GatheringSystem.Clear();Main.netMode=NetmodeID.SinglePlayer;Main.myPlayer=0;
    }
}
