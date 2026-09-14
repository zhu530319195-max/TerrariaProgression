using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ModLoader;
using TerrariaProgression.Core;
using TerrariaProgression.Players;
using TerrariaProgression.Talents;

namespace ProgressionHarness;
public sealed partial class RuntimeChecks
{
    private void RunToolPowerBlast()
    {
        int x=Main.spawnTileX+70,y=Main.spawnTileY-10;
        void Init() {
            Reset();GatheringSystem.Clear();A.Award(1000000000*Experience.Scale);B.Award(1000000000*Experience.Scale);
            Config.EnableWorldGathering=Config.EnableBlastRadius=true;Config.MaxBlocksPerAction=0;Config.GatheringWorkPerTick=256;Config.ProtectedTileAreas.Clear();
            A.Player.inventory[0]=new Item(ItemID.CopperPickaxe);A.Player.selectedItem=0;A.Player.Center=new Vector2(x*16,y*16);A.Player.pickSpeed=1;
            Main.hardMode=false;Main.gameMenu=false;Main.drawingPlayerChat=false;A.Player.mouseInterface=false;
            for(int dx=-26;dx<=26;dx++)for(int dy=-26;dy<=26;dy++)Main.tile[x+dx,y+dy].ClearEverything();
            foreach(var i in Main.item)i.active=false;Array.Clear(Main.timeItemSlotCannotBeReusedFor);
        }
        void Put(int dx,int dy,ushort type=TileID.Stone) {var t=Main.tile[x+dx,y+dy];t.ClearEverything();t.HasTile=true;t.TileType=type;}
        bool Has(int dx,int dy)=>Main.tile[x+dx,y+dy].HasTile;
        void Buy(string id,int n=1)=>Check(A.ApplyTalent(TalentOperation.Upgrade,id,TalentCategory.Utility,n)==TalentResult.Success,"tool/blast purchase: "+id);
        void Toggle(string id,bool on)=>A.ApplyTalent(on?TalentOperation.Enable:TalentOperation.Disable,id,TalentCategory.Utility,1);
        int Items(int type)=>Main.item.Take(Main.maxItems).Where(i=>i.active&&i.type==type).Sum(i=>i.stack);
        void Drain() {for(int n=0;n<5000&&(BlastRadiusSystem.PendingCount>0||GatheringSystem.PendingCount>0);n++)GatheringSystem.ProcessJobs();Check(BlastRadiusSystem.PendingCount==0&&GatheringSystem.PendingCount==0,"combined terrain queue terminates");}
        var tool=typeof(Player).GetMethod("ItemCheck_UseMiningTools_ActuallyUseMiningTool",BindingFlags.Instance|BindingFlags.NonPublic)!;
        void Use(int dx=0,int dy=0) {A.Player.itemTime=0;tool.Invoke(A.Player,new object[]{A.Player.HeldItem,false,x+dx,y+dy});}
        Init();Put(0,0,TileID.Meteorite);
        for(int n=0;n<12;n++)Use();Check(Has(0,0),"copper pick remains below meteorite gate without talent");
        Buy("PickPower",2);for(int n=0;n<12&&Has(0,0);n++)Use();
        Check(!Has(0,0)&&Items(ItemID.Meteorite)==1&&A.Player.HeldItem.pick==35,"native manual pick crosses gate without mutating item");
        Put(0,0,TileID.Hellstone);for(int n=0;n<12;n++)Use();
        Check(Has(0,0),"native pick does not double apply +20 to reach 65-percent hellstone gate");
        Toggle("PickPower",false);Put(0,0,TileID.Meteorite);for(int n=0;n<12;n++)Use();Check(Has(0,0),"disabling restores native gate");
        Toggle("PickPower",true);for(int n=0;n<12&&Has(0,0);n++)GatheringSystem.Hit(A.Player,new(x,y),GatheringMode.Area);
        Check(!Has(0,0),"batch hit shares enhanced pick power");
        Put(0,0,504);Buy("PickPower",1000);for(int n=0;n<12;n++)Use();Check(Has(0,0),"huge pick talent retains native unbreakable tile 504 rule");
        Init();A.Player.inventory[0]=new Item(ItemID.CopperAxe);int original=A.Player.HeldItem.axe;Buy("AxePower",10);
        int capturedDamage=0;
        int DamageSpy(On_HitTile.orig_AddDamage orig,HitTile hit,int buffer,int damage,bool updateAmount) {
            if(ReferenceEquals(hit,A.Player.hitTile))capturedDamage=Math.Max(capturedDamage,damage);
            return orig(hit,buffer,damage,updateAmount);
        }
        On_HitTile.AddDamage+=DamageSpy;
        try {
            Put(0,1,TileID.Grass);Put(0,0,TileID.Trees);Use();
            Check(capturedDamage==(int)((original+20)*1.2f)&&A.Player.HeldItem.axe==original,"native axe applies exactly +100 displayed power then restores item");
            Check(ToolPowerSystem.AxePower(B.Player,A.Player.HeldItem)==original,"same item on another character gets no leaked power");
            capturedDamage=0;Put(0,0,TileID.Trees);GatheringSystem.Hit(A.Player,new(x,y),GatheringMode.Tree);
            Check(capturedDamage==(int)((original+20)*1.2f)&&A.Player.HeldItem.axe==original,"batch axe applies exactly one power bonus");
        } finally {On_HitTile.AddDamage-=DamageSpy;}
        // Binding serialization: the engine starts registered MOD keys empty.
        var keys=new KeyConfiguration();keys.SetupKeys();var markers=new HashSet<string>();
        foreach(var pair in DefaultKeybindRules.Defaults) keys.KeyStatus[pair.Key]=new();
        Check(DefaultKeybindRules.Initialize("Native/Keyboard",keys.KeyStatus,markers),"native empty input profile receives one-time defaults");
        var saved=keys.WritePreferences();var restored=new KeyConfiguration();restored.SetupKeys();foreach(var pair in DefaultKeybindRules.Defaults)restored.KeyStatus.TryAdd(pair.Key,new());restored.ReadPreferences(saved);
        Check(DefaultKeybindRules.Defaults.All(p=>restored.KeyStatus[p.Key].SequenceEqual(new[]{p.Value})),"native preferences persist all three default keys");
        restored.KeyStatus["TerrariaProgression/GatheringAction"].Clear();saved=restored.WritePreferences();keys.SetupKeys();foreach(var pair in DefaultKeybindRules.Defaults)keys.KeyStatus.TryAdd(pair.Key,new());keys.ReadPreferences(saved);
        DefaultKeybindRules.Initialize("Native/Keyboard",keys.KeyStatus,markers);
        Check(keys.KeyStatus["TerrariaProgression/GatheringAction"].Count==0,"native saved intentional clear is not replaced after migration");
        var allowed=new[]{ProjectileID.Bomb,ProjectileID.StickyBomb,ProjectileID.BouncyBomb,ProjectileID.Dynamite,ProjectileID.StickyDynamite,ProjectileID.BouncyDynamite};
        Vector2 nativeCenter=default;int nativeRadius=0;
        void Capture(On_Projectile.orig_ExplodeTiles orig,Projectile p,Vector2 center,int radius,int minX,int maxX,int minY,int maxY,bool walls) {nativeCenter=center;nativeRadius=(int)radius;orig(p,center,radius,minX,maxX,minY,maxY,walls);}
        Projectile SpawnBomb(int type,int owner=0) {int n=Projectile.NewProjectile(new EntitySource_Misc("CI blast"),new Vector2(x*16+11,y*16+11),Vector2.Zero,type,99,1,owner);return Main.projectile[n];}
        void Fill() {for(int dx=-20;dx<=20;dx++)for(int dy=-20;dy<=20;dy++){Put(dx,dy);var tile=Main.tile[x+dx,y+dy];tile.WallType=WallID.Stone;}}
        On_Projectile.ExplodeTiles+=Capture;
        try {
            foreach(int type in allowed) {
                Init();Buy("BlastRadius",3);Fill();var bomb=SpawnBomb(type);bomb.PrepareBombToBlow();
                int radius=BlastRadiusSystem.BaseRadius(type);nativeRadius=0;bomb.Kill();
                Check(nativeRadius==radius && Vector2.Distance(nativeCenter,BlastRadiusSystem.TerrainOrigin(bomb))<0.01f,"native blast origin and base radius match: "+type);
                var core=new HashSet<(int,int)>();var walls=new Dictionary<(int,int),ushort>();
                for(int dx=-20;dx<=20;dx++)for(int dy=-20;dy<=20;dy++){if(!Has(dx,dy))core.Add((dx,dy));walls[(dx,dy)]=Main.tile[x+dx,y+dy].WallType;}
                int damage=bomb.damage,width=bomb.width,height=bomb.height;var npc=Spawn(1000);npc.Center=new Vector2((x+radius+1)*16,y*16);int life=npc.life;
                Check(BlastRadiusSystem.PendingCount==1&&!A.Player.GetModPlayer<GatheringPlayer>().BatchEnabled,"actual projectile death queues extension independently of Alt: "+type);
                Drain();int extra=0;bool exact=true,wallsSame=true;
                for(int dx=-20;dx<=20;dx++)for(int dy=-20;dy<=20;dy++) {
                    double rx=x+dx-nativeCenter.X/16f,ry=y+dy-nativeCenter.Y/16f,d=rx*rx+ry*ry;
                    bool inAnnulus=d>=radius*radius&&d<(radius+3)*(radius+3);
                    if(inAnnulus)extra++;
                    exact &= Has(dx,dy)==(!core.Contains((dx,dy))&&!inAnnulus);
                    wallsSame &= Main.tile[x+dx,y+dy].WallType==walls[(dx,dy)];
                }
                Check(exact,"all 1681 cells match exact terrain annulus: "+type);
                Check(wallsSame,"all 1681 wall cells unchanged by extra annulus: "+type);
                Check(extra>0&&bomb.damage==damage&&bomb.width==width&&bomb.height==height&&npc.life==life,"extra blast never changes damage fields or hurts NPCs: "+type);
                bomb.GetGlobalProjectile<BlastRadiusProjectile>().OnKill(bomb,0);Check(BlastRadiusSystem.PendingCount==0,"duplicate death cannot enqueue again: "+type);
            }
        } finally {On_Projectile.ExplodeTiles-=Capture;}
        foreach(int type in new[]{ProjectileID.Grenade,ProjectileID.BombFish,ProjectileID.ScarabBomb,ProjectileID.RocketI,ProjectileID.DirtBomb,ProjectileID.WetBomb}) {
            Init();Buy("BlastRadius",10);var bomb=SpawnBomb(type);
            Check(!BlastRadiusSystem.Enqueue(bomb,A.SessionId),"non-whitelisted explosive excluded: "+type);
        }
        Init();Buy("BlastRadius",3);var probe=SpawnBomb(ProjectileID.Bomb);probe.Center=new Vector2(x*16+11,y*16+11);
        Put(5,0,TileID.Meteorite);Put(-5,0,TileID.Hellstone);Put(0,5,TileID.BlueDungeonBrick);Put(0,-5,TileID.LihzahrdBrick);Put(4,4);var wire=Main.tile[x+4,y+4];wire.RedWire=true;
        Put(-4,4);Config.ProtectedTileAreas.Add(new(){X=x-4,Y=y+4});Put(4,-4,TileID.Copper);Put(-4,-4);
        Check(BlastRadiusSystem.Enqueue(probe,A.SessionId),"server-checked blast accepted");Drain();
        Check(Has(5,0)&&Has(-5,0)&&Has(0,5)&&Has(0,-5)&&Has(4,4)&&Has(-4,4)&&!Has(4,-4)&&!Has(-4,-4),"extra blast retains prehard ore gates, dungeon, temple, wires and configured protection");
        Init();Buy("BlastRadius",10);Fill();probe=SpawnBomb(ProjectileID.Bomb);Config.MaxBlocksPerAction=5;Config.GatheringWorkPerTick=1;BlastRadiusSystem.Enqueue(probe,A.SessionId);
        int Before()=>Enumerable.Range(-20,41).Sum(dx=>Enumerable.Range(-20,41).Count(dy=>Has(dx,dy)));
        int before=Before();for(int n=0;n<100;n++){int a=Before();GatheringSystem.ProcessJobs();Check(a-Before()<=1,"blast consumes at most one removal per frame budget unit");}Drain();
        Check(before-Before()==5,"blast expansion stops exactly at additional block limit");
        Init();Buy("BlastRadius",3);probe=SpawnBomb(ProjectileID.Bomb);Put(5,0);BlastRadiusSystem.Enqueue(probe,A.SessionId);Toggle("BlastRadius",false);Drain();Check(Has(5,0),"disabling cancels queued blast");
        Init();Buy("BlastRadius",3);probe=SpawnBomb(ProjectileID.Bomb);Put(5,0);BlastRadiusSystem.Enqueue(probe,A.SessionId);A.SessionReady=false;Drain();Check(Has(5,0),"session ending cancels queued blast");
        Init();Buy("BlastRadius",3);probe=SpawnBomb(ProjectileID.Bomb);Put(5,0);Main.netMode=NetmodeID.MultiplayerClient;Check(!BlastRadiusSystem.Enqueue(probe,A.SessionId)&&Has(5,0),"client cannot perform extra terrain destruction");
        Main.netMode=NetmodeID.Server;Main.myPlayer=255;Check(BlastRadiusSystem.Enqueue(probe,A.SessionId),"dedicated server accepts actual projectile owner");Drain();Check(!Has(5,0)&&Items(ItemID.StoneBlock)==1,"server alone removes and drops extra block");
        Init();Buy("BlastRadius",3);probe=SpawnBomb(ProjectileID.Bomb);Config.EnableBlastRadius=false;Check(!BlastRadiusSystem.Enqueue(probe,A.SessionId),"blast permission gates enqueue");Config.EnableBlastRadius=true;
        Check(!BlastRadiusSystem.Enqueue(probe,Guid.NewGuid()),"stale projectile session rejected");probe.owner=1;Check(!BlastRadiusSystem.Enqueue(probe,B.SessionId),"other player's projectile cannot borrow talent");
        Init();Buy("BlastRadius",3);probe=SpawnBomb(ProjectileID.Bomb);Put(5,0);BlastRadiusSystem.Enqueue(probe,A.SessionId);probe.type=ProjectileID.DirtBomb;probe.position=Vector2.Zero;Drain();Check(!Has(5,0),"queued blast keeps immutable origin and type when projectile slot changes");
        GatheringSystem.Clear();Config.MaxBlocksPerAction=1000;Config.GatheringWorkPerTick=32;Config.ProtectedTileAreas.Clear();Main.netMode=NetmodeID.SinglePlayer;Main.myPlayer=0;
    }
}
