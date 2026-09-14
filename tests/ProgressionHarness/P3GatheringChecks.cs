using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Core;
using TerrariaProgression.Config;
using TerrariaProgression.Talents;
using TerrariaProgression.Networking;

namespace ProgressionHarness;
public sealed partial class RuntimeChecks
{
    private void RunP3Gathering()
    {
        int x=Main.spawnTileX+35,y=Main.spawnTileY-10;
        uint seq=0;
        void Advance(ulong ticks=100)=>typeof(Main).GetProperty(nameof(Main.GameUpdateCount))!.SetValue(null,Main.GameUpdateCount+ticks);
        void Init() {
            Reset(); GatheringSystem.Clear(); seq=0; A.Award(100000000*Experience.Scale);
            Config.EnableWorldGathering=Config.EnableAreaMining=Config.EnableVeinMining=Config.EnableTreeFelling=true;
            Config.EnableModOreMining=false; Config.MaxBlocksPerAction=1000; Config.GatheringWorkPerTick=32; Config.ProtectedTileAreas.Clear();
            A.Player.inventory[0]=new Item(ItemID.PickaxeAxe);A.Player.selectedItem=0;A.Player.pickSpeed=1;
            A.Player.Center=new Vector2(x*16,y*16);
            for(int dx=-15;dx<=20;dx++)for(int dy=-20;dy<=15;dy++)Main.tile[x+dx,y+dy].ClearEverything();
            foreach(var i in Main.item)i.active=false; Array.Clear(Main.timeItemSlotCannotBeReusedFor);
        }
        void Put(int dx,int dy,ushort type) { var t=Main.tile[x+dx,y+dy];t.ClearEverything();t.HasTile=true;t.TileType=type; }
        bool Has(int dx,int dy)=>Main.tile[x+dx,y+dy].HasTile;
        void Buy(string id,int n=1)=>Check(A.ApplyTalent(TalentOperation.Upgrade,id,TalentCategory.Utility,n)==TalentResult.Success,"purchase gathering fixture: "+id);
        bool Request(GatheringMode mode,int dx=0,int dy=0) {
            Advance();
            return GatheringSystem.Request(A.Player,mode,x+dx,y+dy,A.SessionId,A.TalentRevision,++seq,0,A.Player.HeldItem.type);
        }
        void Drain() { for(int n=0;n<2000&&GatheringSystem.PendingCount>0;n++){Advance(1);GatheringSystem.ProcessJobs();}Check(GatheringSystem.PendingCount==0,"gathering queue terminates"); }
        int Items(int type)=>Main.item.Take(Main.maxItems).Where(i=>i.active&&i.type==type).Sum(i=>i.stack);
        Init();Buy("AreaMining");
        for(int dx=-2;dx<=2;dx++)for(int dy=-2;dy<=2;dy++)Put(dx,dy,TileID.Stone);
        Check(Request(GatheringMode.Area)&&!Has(0,0)&&Has(1,0),"area waits for first break and defers neighbours");Drain();
        Check(Items(ItemID.StoneBlock)==9 && !Has(1,1)&&Has(2,0)&&Has(-2,-2),"Lv1 mines exactly 3x3 with one native drop per tile");
        Init();Buy("AreaMining");A.Player.inventory[0]=new Item(ItemID.CopperPickaxe);Put(0,0,TileID.Stone);Put(1,0,TileID.Stone);
        Check(Request(GatheringMode.Area)&&Has(0,0)&&GatheringSystem.PendingCount==0,"weak tool must finish native first-tile hit damage");
        for(int n=0;n<8&&Has(0,0);n++)Request(GatheringMode.Area);Drain();Check(!Has(0,0)&&!Has(1,0),"native accumulated pick damage eventually starts area job");
        Init();Buy("AreaMining");A.Player.inventory[0]=new Item(ItemID.CopperPickaxe);Put(0,0,TileID.Stone);Put(1,0,TileID.Meteorite);
        for(int n=0;n<8&&Has(0,0);n++)Request(GatheringMode.Area);Drain();Check(Has(1,0)&&Items(ItemID.Meteorite)==0,"area cannot bypass native meteorite pick-power gate");
        Init();Buy("VeinMining");Buy("MiningYield",10);Put(0,0,TileID.Copper);Put(1,1,TileID.Copper);Put(2,2,TileID.Copper);Put(4,4,TileID.Copper);Put(1,0,TileID.Tin);Put(0,1,TileID.Stone);
        Request(GatheringMode.Vein);Drain();Check(!Has(2,2)&&Has(4,4)&&Has(1,0)&&Has(0,1),"vein crosses diagonals but not gaps or different blocks");
        Check(Items(ItemID.CopperOre)==6,"mining yield doubles three real ore drops once");
        Init();Buy("AreaMining",10);Config.MaxBlocksPerAction=3;Config.GatheringWorkPerTick=1;
        for(int dx=-2;dx<=2;dx++)for(int dy=-2;dy<=2;dy++)Put(dx,dy,TileID.Stone);
        Request(GatheringMode.Area);int before=Items(ItemID.StoneBlock);GatheringSystem.ProcessJobs();
        Check(Items(ItemID.StoneBlock)-before<=1 && GatheringSystem.PendingCount==1,"global one-work budget cannot clear whole area in one tick");Drain();
        Check(Items(ItemID.StoneBlock)==3 && A.State.Talents["AreaMining"].TalentLevel==10,"action limit includes origin without reducing paid level");
        Init();Buy("AreaMining");Config.MaxBlocksPerAction=0;
        for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)Put(dx,dy,TileID.Stone);
        Request(GatheringMode.Area);Drain();Check(Items(ItemID.StoneBlock)==9,"zero action cap still completes through queue");
        Init();Buy("AreaMining");Put(0,0,TileID.Stone);Put(1,0,TileID.Stone);var wired=Main.tile[x+1,y];wired.RedWire=true;
        Request(GatheringMode.Area);Drain();Check(Has(1,0),"wired tile remains protected");
        Init();Buy("AreaMining");Put(0,0,TileID.Stone);Put(1,0,TileID.Stone);Put(2,0,TileID.Containers);
        Request(GatheringMode.Area);Drain();Check(Has(1,0)&&Has(2,0),"chest and adjacent support survive area operation");
        Init();Buy("AreaMining");Put(0,0,TileID.Stone);Put(0,-1,TileID.Sand);
        Check(!Request(GatheringMode.Area)&&Has(0,0),"falling-block supports are refused before destruction");
        Init();Buy("AreaMining");Put(0,0,TileID.Stone);Config.ProtectedTileAreas.Add(new ProtectedTileArea{X=x,Y=y,Width=1,Height=1});
        Check(!Request(GatheringMode.Area)&&Has(0,0),"configured rectangle blocks even the initial tile");
        Config.ProtectedTileAreas.Clear();Config.EnableWorldGathering=false;
        Check(!Request(GatheringMode.Area)&&Has(0,0),"server master switch blocks operation");
        Config.EnableWorldGathering=true;A.ApplyTalent(TalentOperation.Disable,"AreaMining",TalentCategory.Utility,1);
        Check(!Request(GatheringMode.Area)&&Has(0,0),"disabled talent cannot start operation");
        Init();Buy("AreaMining");Put(0,0,TileID.Stone);Put(1,0,TileID.Stone);Request(GatheringMode.Area);A.Player.inventory[0]=new Item(ItemID.WoodenSword);Drain();
        Check(Has(1,0),"switching away from mining tool cancels remaining work");
        Init();Buy("AreaMining");Put(0,0,TileID.Stone);Put(1,0,TileID.Stone);Request(GatheringMode.Area);
        A.ApplyTalent(TalentOperation.RefundTalent,"AreaMining",TalentCategory.Utility,1);Drain();Check(Has(1,0),"refund cancels queued work");
        Init();Buy("AreaMining");Put(0,0,TileID.Stone);Put(1,0,TileID.Stone);Main.netMode=NetmodeID.MultiplayerClient;
        Check(!Request(GatheringMode.Area)&&Has(0,0),"client cannot execute authoritative world request");
        Main.netMode=NetmodeID.Server;Advance();
        // Exercise actual packet decoder instead of only a direct internal request.
        void Packet(Guid session,uint request,int tx,int slot) {
            using var bytes=new MemoryStream();using var w=new BinaryWriter(bytes);
            w.Write(ProgressionNetwork.ProtocolVersion);w.Write((byte)ProgressionMessage.GatheringAction);
            w.Write(session.ToByteArray());w.Write(A.TalentRevision);w.Write(request);w.Write((byte)GatheringMode.Area);
            w.Write(tx);w.Write(y);w.Write(slot);w.Write(A.Player.HeldItem.type);w.Flush();bytes.Position=0;
            ProgressionNetwork.Receive(new BinaryReader(bytes),0);
        }
        Packet(Guid.NewGuid(),1,x,0);Check(Has(0,0),"stale session packet cannot modify world");
        Packet(A.SessionId,2,x,1);Check(Has(0,0),"spoofed selected slot cannot modify world");
        Packet(A.SessionId,3,x+100,0);Check(Has(0,0),"out-of-reach packet cannot modify world");Advance();
        Packet(A.SessionId,4,x,0);Check(!Has(0,0)&&Has(1,0),"server decodes valid intent and owns initial break");Drain();
        Check(Items(ItemID.StoneBlock)==2,"server alone creates each real drop");Put(0,0,TileID.Stone);Advance();
        Packet(A.SessionId,4,x,0);Check(Has(0,0),"replayed accepted request cannot break a replacement tile");
        // Native tree frames: hit the middle and finish the lower stump too.
        Init();Buy("TreeFelling");Buy("WoodYield",10);A.Player.inventory[0]=new Item(ItemID.PickaxeAxe);
        for(int dx=0;dx<=3;dx+=3){Put(dx,5,TileID.Grass);for(int dy=-4;dy<=4;dy++)Put(dx,dy,TileID.Trees);}
        for(int n=0;n<10&&Has(0,0);n++)Request(GatheringMode.Tree);Drain();
        Check(Enumerable.Range(-4,9).All(dy=>!Has(0,dy))&&Enumerable.Range(-4,9).All(dy=>Has(3,dy)),"one tree including lower stump cleared, adjacent same-type tree preserved");
        Check(Items(ItemID.Wood)==18,"nine tree tiles use existing doubled wood yield once");
        Init();Buy("TreeFelling");Put(0,5,TileID.Grass);for(int dy=-4;dy<=4;dy++)Put(0,dy,TileID.Trees);
        Config.MaxBlocksPerAction=3;Check(!Request(GatheringMode.Tree)&&Has(0,0),"over-budget whole tree is refused intact");
        Config.MaxBlocksPerAction=1000;Config.ProtectedTileAreas.Add(new ProtectedTileArea{X=x,Y=y-4,Width=1,Height=1});
        Check(!Request(GatheringMode.Tree)&&Has(0,0),"protected crown refuses whole tree before starting");
        Init();Buy("TreeFelling");Put(0,0,TileID.WoodBlock);Check(!Request(GatheringMode.Tree)&&Has(0,0),"tree mode cannot fell wooden construction");
        GatheringSystem.Clear();Config.ProtectedTileAreas.Clear();Config.MaxBlocksPerAction=1000;Config.GatheringWorkPerTick=32;
        Main.netMode=NetmodeID.SinglePlayer;
    }

    private void RunFlexibleRange()
    {
        Reset();A.Award(100000000*Experience.Scale);var p=A.Player;
        p.Center=new Vector2(Main.spawnTileX*16,(Main.spawnTileY-15)*16);p.direction=1;
        A.ApplyTalent(TalentOperation.Upgrade,"MeleeRange",TalentCategory.Combat,10);
        foreach(int itemType in new[]{ItemID.BlandWhip,ItemID.FireWhip,ItemID.RainbowWhip}) {
            p.inventory[0]=new Item(itemType);p.selectedItem=0;p.itemAnimationMax=p.itemAnimation=p.HeldItem.useAnimation;
            int index=Projectile.NewProjectile(p.GetSource_ItemUse(p.HeldItem),p.Center,new Vector2(4,0),p.HeldItem.shoot,20,0,p.whoAmI);
            var q=Main.projectile[index];q.ai[0]=p.itemAnimationMax*.5f;q.spriteDirection=1;
            var native=new List<Vector2>();var enlarged=new List<Vector2>();
            A.State.Talents["MeleeRange"].Enabled=false;Projectile.FillWhipControlPoints(q,native);
            A.State.Talents["MeleeRange"].Enabled=true;Projectile.FillWhipControlPoints(q,enlarged);
            Vector2 origin=native[0];
            Check(native.Count==enlarged.Count&&native.Zip(enlarged).All(pair=>Vector2.Distance(origin+(pair.First-origin)*2,pair.Second)<.1f),"native whip visible and collision control points double: "+itemType);
            var far=enlarged.MaxBy(v=>Vector2.DistanceSquared(v,origin));var target=new Rectangle((int)far.X-2,(int)far.Y-2,4,4);
            Check(q.Colliding(q.Hitbox,target),"enlarged whip reaches actual native collision target: "+itemType);
            A.State.Talents["MeleeRange"].Enabled=false;Check(!q.Colliding(q.Hitbox,target),"disabled whip no longer hits distant target: "+itemType);
            A.State.Talents["MeleeRange"].Enabled=true;Check(q.damage==20,"range never changes whip damage: "+itemType);q.active=false;
        }
        foreach(int itemType in new[]{ItemID.BallOHurt,ItemID.BlueMoon,ItemID.DaoofPow,ItemID.FlowerPow}) {
            p.inventory[0]=new Item(itemType);p.selectedItem=0;p.GetAttackSpeed(DamageClass.Melee)=1;p.controlUseItem=false;
            float Run(bool enabled,bool launch) {
                A.State.Talents["MeleeRange"].Enabled=enabled;p.channel=!launch;
                int index=Projectile.NewProjectile(p.GetSource_ItemUse(p.HeldItem),p.MountedCenter,Vector2.Zero,p.HeldItem.shoot,20,0,p.whoAmI);
                var q=Main.projectile[index];float furthest=0;
                for(int i=0;i<(launch?30:5);i++) {
                    ProjectileLoader.ProjectileAI(q);if(!q.active)break;
                    q.position+=q.velocity;furthest=Math.Max(furthest,Vector2.Distance(q.Center,p.MountedCenter));
                }
                if(!launch)Check(q.Colliding(q.Hitbox,new Rectangle((int)p.MountedCenter.X+80,(int)p.MountedCenter.Y,1,1))==enabled,"native flail spin collision respects enabled range: "+itemType);
                q.active=false;return furthest;
            }
            float n=Run(false,false),e=Run(true,false);Check(Math.Abs(e/n-2)<.01,"flail visible spin distance doubles without accumulation: "+itemType);
            n=Run(false,true);e=Run(true,true);Check(e>n*1.8f&&e<n*2.2f,"flail actual launched distance approximately doubles with native return: "+itemType);
        }
        A.State.Talents["MeleeRange"].CurrentIntensity=3;
        var probe=new Projectile();probe.SetDefaults(ProjectileID.Bullet);probe.owner=0;
        Check(!FlexibleRangeProjectile.Flail(probe),"ordinary bullets are not flail-adapted");
        Check(Math.Abs(FlexibleRangeProjectile.Factor(probe)-1.3)<.001,"flexible range reads reduced current intensity");
        A.State.Talents["MeleeRange"].Enabled=false;Check(FlexibleRangeProjectile.Factor(probe)==1,"flexible range disable restores native factor");
        Main.netMode=NetmodeID.SinglePlayer;
    }
}
