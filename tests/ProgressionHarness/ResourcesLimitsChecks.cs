using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using TerrariaProgression.Config;
using TerrariaProgression.Core;
using TerrariaProgression.Networking;
using TerrariaProgression.Players;
using TerrariaProgression.Talents;

namespace ProgressionHarness;
public sealed partial class RuntimeChecks
{
    private void RunResourcesLimits()
    {
        int x=Main.spawnTileX+180,y=Main.spawnTileY-15;uint sequence=0;
        void Advance()=>typeof(Main).GetField("_gameUpdateCount",BindingFlags.NonPublic|BindingFlags.Static)!.SetValue(null,unchecked(Main.GameUpdateCount+100));
        void Init() {
            Reset();GatheringSystem.Clear();TreeReplantSystem.Clear();sequence=0;
            Config.GlobalTalentLevelLimit="-1";Config.TalentLevelOverrides.Clear();Config.OnChanged();
            Config.EnableWorldGathering=Config.EnableAreaWallRemoval=Config.EnableAutoReplantTree=Config.EnableHerbGrowth=true;
            Config.EnableTreeFelling=true;Config.MaxBlocksPerAction=1000;Config.GatheringWorkPerTick=32;Config.ProtectedTileAreas.Clear();
            A.Award(1000000000*Experience.Scale);B.Award(1000000000*Experience.Scale);
            A.Player.Center=new Vector2(x*16,y*16);B.Player.Center=A.Player.Center+new Vector2(1600,0);
            A.Player.inventory[0]=new Item(ItemID.PickaxeAxe);A.Player.selectedItem=0;A.Player.pickSpeed=1;
            A.Player.GetModPlayer<GatheringPlayer>().BatchEnabled=true;
            Main.gameMenu=false;Main.drawingPlayerChat=false;A.Player.mouseInterface=false;
            Main.dayTime=true;Main.time=10000;Main.raining=false;Main.bloodMoon=false;Main.moonPhase=4;
            for(int dx=-25;dx<=25;dx++)for(int dy=-35;dy<=20;dy++)Main.tile[x+dx,y+dy].ClearEverything();
            foreach(var item in Main.item)item.active=false;Array.Clear(Main.timeItemSlotCannotBeReusedFor);Advance();
        }
        void Buy(string id,int n=1)=>Check(A.ApplyTalent(TalentOperation.Upgrade,id,TalentCategory.Utility,n)==TalentResult.Success,"resource/limits purchase: "+id);
        void Put(int dx,int dy,ushort type) {var t=Main.tile[x+dx,y+dy];t.ClearEverything();t.HasTile=true;t.TileType=type;}
        void Wall(int dx,int dy,ushort type=WallID.Wood) {var t=Main.tile[x+dx,y+dy];t.WallType=type;}
        int Items(int type)=>Main.item.Take(Main.maxItems).Where(i=>i.active&&i.type==type).Sum(i=>i.stack);
        bool Request(GatheringMode mode,int dx=0,int dy=0) {Advance();return GatheringSystem.Request(A.Player,mode,x+dx,y+dy,A.SessionId,A.TalentRevision,++sequence,0,A.Player.HeldItem.type);}
        void Drain(){for(int n=0;n<3000&&GatheringSystem.PendingCount>0;n++)GatheringSystem.ProcessJobs();Check(GatheringSystem.PendingCount==0,"new world job terminates");TreeReplantSystem.Process();}

        Init();Buy("AreaMining",20);Buy("NoFallDamage");Buy("AllInformation");
        A.ApplyTalent(TalentOperation.DecreaseIntensity,"AreaMining",TalentCategory.Utility,5);
        byte[] saved=StateCodec.Encode(A.State);Config.GlobalTalentLevelLimit="10";Config.OnChanged();
        Check(ExtendedTalentPlayer.Level(A.Player,"AreaMining")==10&&StateCodec.Encode(A.State).SequenceEqual(saved),"live config clips effect without modifying character");
        Config.GlobalTalentLevelLimit="0";Config.OnChanged();
        A.Player.noFallDmg=false;A.Player.GetModPlayer<FunctionalTalentPlayer>().UpdateEquips();
        Check(!A.Player.noFallDmg&&!FunctionalTalentRegistry.ChildEnabled(A.State,"AllInformation","Time"),"server zero reaches functional flags and composite children");
        var tag=new TagCompound();A.SaveData(tag);B.LoadData(tag);
        Check(StateCodec.Encode(B.State).SequenceEqual(saved)&&TalentCatalog.ValidateImported(B.State),"above-cap save reload and import preserve full ledger");
        Config.TalentLevelOverrides.Add(new(){TalentId="AreaMining",Limit="-1"});Config.OnChanged();
        Check(ExtendedTalentPlayer.Level(A.Player,"AreaMining")==15&&ExtendedTalentPlayer.Level(A.Player,"NoFallDamage")==0,"global zero selectively opens explicit unlimited override");
        Config.TalentPointsPerLevel="7";Config.MaxBlocksPerAction=57;
        Config.RestoreDefaultTalentLimits=true;Config.OnChanged();
        Check(Config.GlobalTalentLevelLimit=="-1"&&Config.TalentLevelOverrides.Count==0&&Config.TalentPointsPerLevel=="7"&&Config.MaxBlocksPerAction==57&&StateCodec.Encode(A.State).SequenceEqual(saved),"restore command only resets limit settings");
        Check(ExtendedTalentPlayer.Level(A.Player,"NoFallDamage")==1&&ExtendedTalentPlayer.Level(A.Player,"AreaMining")==15,"restore re-enables original saved strengths");
        Main.netMode=NetmodeID.Server;var message=Terraria.Localization.NetworkText.Empty;
        Check(!Config.AcceptClientChanges(new ProgressionConfig(),0,ref message),"restore does not weaken existing server config permissions");Main.netMode=NetmodeID.SinglePlayer;

        Init();Buy("BasicBlockYield",10);Buy("MiningYield",10);Buy("GemYield",10);Buy("WoodYield",10);
        Check(BasicBlockResources.Items.Count==30,"exact approved 30 basic block items");
        foreach(int itemId in BasicBlockResources.Items) {
            var item=new Item(itemId);int tile=item.createTile;
            Check(tile>=0&&EconomySystem.Resource(tile,item)=="BasicBlockYield","placed basic block classifies once: "+itemId);
            foreach(var i in Main.item)i.active=false;Array.Clear(Main.timeItemSlotCannotBeReusedFor);
            Put(0,1,TileID.Stone);Put(0,0,(ushort)tile);
            for(int n=0;n<20&&Main.tile[x,y].HasTile;n++)A.Player.PickTile(x,y,A.Player.HeldItem.pick);
            Check(!Main.tile[x,y].HasTile&&Items(itemId)==2,"ordinary pick doubles real basic block drop once: "+itemId+", got "+Items(itemId));
        }
        Check(EconomySystem.Resource(TileID.Copper,new Item(ItemID.CopperOre))=="MiningYield"&&EconomySystem.Resource(TileID.Amethyst,new Item(ItemID.Amethyst))=="GemYield","ore and gem adapters remain mutually exclusive with basic blocks");
        Check(EconomySystem.Resource(TileID.Trees,new Item(ItemID.Wood))=="WoodYield"&&EconomySystem.Resource(TileID.WoodBlock,new Item(ItemID.Wood))==null,"wood and crafted blocks retain old scope");
        Init();Buy("BasicBlockYield",10);Buy("AreaMining");Put(0,0,TileID.Dirt);Put(1,0,TileID.Dirt);Request(GatheringMode.Area);Drain();
        Check(Items(ItemID.DirtBlock)==4,"range mining applies basic yield once per actual drop");
        Init();Buy("BasicBlockYield",10);Put(0,0,TileID.Dirt);
        int bombIndex=Projectile.NewProjectile(A.Player.GetSource_Misc("resource blast"),new Vector2(x*16+11,y*16+11),Vector2.Zero,ProjectileID.Bomb,10,0,0);
        var bomb=Main.projectile[bombIndex];bomb.Center=new Vector2(x*16+11,y*16+11);bomb.velocity=Vector2.Zero;
        Main.netMode=NetmodeID.Server;Main.myPlayer=255;bomb.Kill();
        Check(!Main.tile[x,y].HasTile&&Items(ItemID.DirtBlock)==2,"server bomb attributes basic yield without buying blast radius");
        bomb.GetGlobalProjectile<BlastRadiusProjectile>().OnKill(bomb,0);Check(Items(ItemID.DirtBlock)==2,"duplicate bomb callback cannot duplicate basic yield");

        Init();Buy("HammerPower",2);A.Player.inventory[0]=new Item(ItemID.WoodenHammer);
        var wallMethod=typeof(Player).GetMethod("ItemCheck_UseMiningTools_TryHittingWall",BindingFlags.Instance|BindingFlags.NonPublic)!;
        int damageSeen=0;
        void Spy(On_Player.orig_PickWall orig,Player p,int tx,int ty,int damage) {damageSeen=damage;orig(p,tx,ty,damage);}
        On_Player.PickWall+=Spy;
        try {
            Wall(0,0);A.Player.controlUseItem=true;A.Player.itemAnimation=30;A.Player.toolTime=0;
            int original=A.Player.HeldItem.hammer;
            wallMethod.Invoke(A.Player,new object[]{A.Player.HeldItem,x,y});
            Check(damageSeen==(int)((original+20)*1.5f)&&A.Player.HeldItem.hammer==original,"actual manual wall entry adds hammer power exactly once and restores item");
        } finally {On_Player.PickWall-=Spy;}
        Init();Buy("AreaWallRemoval");Buy("HammerPower",10);A.Player.inventory[0]=new Item(ItemID.WoodenHammer);
        for(int dx=-2;dx<=2;dx++)for(int dy=-2;dy<=2;dy++)Wall(dx,dy);
        Put(1,0,TileID.Stone);Wall(1,0);var foreground=Main.tile[x+1,y];foreground.Slope=Terraria.ID.SlopeType.SlopeDownRight;
        Check(Request(GatheringMode.Wall),"wall request accepted");Drain();
        Check(Enumerable.Range(-1,3).All(dx=>Enumerable.Range(-1,3).All(dy=>Main.tile[x+dx,y+dy].WallType==WallID.None))&&Main.tile[x+2,y].WallType==WallID.Wood,"wall radius one removes exactly 3x3");
        Check(Main.tile[x+1,y].HasTile&&Main.tile[x+1,y].Slope==Terraria.ID.SlopeType.SlopeDownRight,"wall batch preserves foreground and slope");
        Init();Buy("AreaWallRemoval",10);Buy("HammerPower",10);A.Player.inventory[0]=new Item(ItemID.WoodenHammer);Wall(0,0);Wall(10,10);Wall(11,0);Request(GatheringMode.Wall);Drain();
        Check(Main.tile[x+10,y+10].WallType==WallID.None&&Main.tile[x+11,y].WallType==WallID.Wood,"level ten wall radius is 21x21");
        Init();Buy("AreaWallRemoval");A.Player.inventory[0]=new Item(ItemID.WoodenHammer);
        for(int dx=-2;dx<=2;dx++)for(int dy=-2;dy<=2;dy++)Wall(dx,dy,WallID.DirtUnsafe);
        Check(!Request(GatheringMode.Wall)&&Main.tile[x,y].WallType==WallID.DirtUnsafe,"interior natural wall cannot bypass native edge requirement");
        Init();Buy("AreaWallRemoval",10);Buy("HammerPower",10);A.Player.inventory[0]=new Item(ItemID.WoodenHammer);Wall(0,0);Wall(1,0);Request(GatheringMode.Wall);
        Config.GlobalTalentLevelLimit="0";Config.OnChanged();Drain();Check(Main.tile[x+1,y].WallType==WallID.Wood,"live zero cap cancels pending wall job");
        Init();Buy("AreaWallRemoval");A.Player.inventory[0]=new Item(ItemID.WoodenHammer);Wall(0,0);
        Check(!GatheringSystem.Request(A.Player,GatheringMode.Wall,x,y,Guid.NewGuid(),A.TalentRevision,1,0,A.Player.HeldItem.type),"stale wall session rejected");
        Main.netMode=NetmodeID.MultiplayerClient;Check(!Request(GatheringMode.Wall)&&Main.tile[x,y].WallType==WallID.Wood,"client cannot execute wall world mutation");

        Init();Buy("HerbGrowth",10);B.ApplyTalent(TalentOperation.Upgrade,"HerbGrowth",TalentCategory.Utility,5);B.Player.Center=A.Player.Center;
        void Herb(int style,ushort stage) {Put(0,1,TileID.PlanterBox);Put(0,0,stage);var t=Main.tile[x,y];t.TileFrameX=(short)(style*18);}
        Herb(0,TileID.ImmatureHerbs);
        Check(HerbGrowthSystem.Strongest(x,y)==10,"overlapping players use highest growth instead of sum");
        A.Player.Center=new Vector2((x+.5f+50)*16,(y+.5f)*16);B.Player.dead=true;
        Check(HerbGrowthSystem.Strongest(x,y)==10,"growth includes radius boundary");A.Player.Center+=new Vector2(1,0);
        Check(HerbGrowthSystem.Strongest(x,y)==0,"growth excludes one pixel outside radius");A.Player.Center=new Vector2((x+.5f)*16,(y+.5f)*16);
        for(int style=0;style<7;style++)foreach(ushort stage in new[]{TileID.ImmatureHerbs,TileID.MatureHerbs,TileID.BloomingHerbs})for(int seed=0;seed<20;seed++) {
            Herb(style,stage);Config.EnableHerbGrowth=false;_ = WorldGen.genRand;WorldGen._genRand=new UnifiedRandom(seed);
            for(int n=0;n<3;n++)WorldGen.GrowAlch(x,y);
            var expected=(Main.tile[x,y].HasTile,Main.tile[x,y].TileType,Main.tile[x,y].TileFrameX,WorldGen.IsHarvestableHerbWithSeed(Main.tile[x,y].TileType,style));
            Herb(style,stage);Config.EnableHerbGrowth=true;WorldGen._genRand=new UnifiedRandom(seed);Advance();WorldGen.GrowAlch(x,y);
            var actual=(Main.tile[x,y].HasTile,Main.tile[x,y].TileType,Main.tile[x,y].TileFrameX,WorldGen.IsHarvestableHerbWithSeed(Main.tile[x,y].TileType,style));
            Check(actual==expected,"growth equals three native herb updates, including bloom rules: "+style+"/"+stage+"/"+seed);
        }
        Main.netMode=NetmodeID.MultiplayerClient;Check(HerbGrowthSystem.Strongest(x,y)==0,"client has no extra growth authority");

        foreach(int tree in new[]{TileID.Trees,TileID.PalmTree,TileID.TreeAmethyst,TileID.TreeTopaz,TileID.TreeSapphire,TileID.TreeEmerald,TileID.TreeRuby,TileID.TreeDiamond,TileID.TreeAmber}) {
            Init();Buy("AutoReplantTree");int seed=TreeReplantSystem.Seed(tree);A.Player.inventory[1]=new Item(seed,3);
            Put(0,5,tree==TileID.Trees?TileID.Grass:tree==TileID.PalmTree?TileID.Sand:TileID.Stone);Put(0,6,TileID.Stone);
            Check(TreeReplantSystem.Replant(A.Player,new(x,y+5),tree,new Dictionary<int,Item>())&&A.Player.inventory[1].stack==2,"native sapling object placed using matching material: "+tree);
            Check(!TreeReplantSystem.Replant(A.Player,new(x,y+5),tree,new Dictionary<int,Item>())&&A.Player.inventory[1].stack==2,"existing sapling avoids duplicate material cost: "+tree);
        }
        Init();Buy("AutoReplantTree");Put(0,5,TileID.Grass);A.Player.inventory[1]=new Item(ItemID.Acorn,5);
        int freshIndex=Item.NewItem(A.Player.GetSource_Misc("fresh"),A.Player.Hitbox,ItemID.Acorn,2);
        Check(TreeReplantSystem.Replant(A.Player,new(x,y+5),TileID.Trees,new Dictionary<int,Item>{{freshIndex,Main.item[freshIndex]}})&&A.Player.inventory[1].stack==5&&Main.item[freshIndex].stack==1,"fresh harvest preferred over inventory for tree replant");
        Init();Buy("AutoReplantTree");Put(0,5,TileID.Stone);A.Player.inventory[1]=new Item(ItemID.Acorn,5);
        Check(!TreeReplantSystem.Replant(A.Player,new(x,y+5),TileID.TreeRuby,new Dictionary<int,Item>())&&A.Player.inventory[1].stack==5,"ordinary acorn cannot replace matching gemcorn");
        Check(!TreeReplantSystem.Replant(A.Player,new(x,y+5),TileID.Trees,new Dictionary<int,Item>())&&A.Player.inventory[1].stack==5,"illegal substrate costs no material");
        foreach(bool batch in new[]{false,true}) {
            Init();Buy("AutoReplantTree");if(batch)Buy("TreeFelling");A.Player.inventory[1]=new Item(ItemID.Acorn,5);A.Player.HeldItem.axe=100;
            for(int dx=-3;dx<=3;dx++){Put(dx,6,TileID.Stone);Put(dx,5,TileID.Grass);}
            Check(WorldGen.GrowTree(x,y+5),"actual tree fixture grows for replant pipeline");
            A.Player.GetModPlayer<GatheringPlayer>().BatchEnabled=batch;
            if(batch){Request(GatheringMode.Tree,0,2);Drain();}
            else {
                var tool=typeof(Player).GetMethod("ItemCheck_UseMiningTools_ActuallyUseMiningTool",BindingFlags.Instance|BindingFlags.NonPublic)!;
                var oldActor=EconomySystem.Actor;EconomySystem.Actor=A.Player;
                try {tool.Invoke(A.Player,new object[]{A.Player.HeldItem,false,x,y+2});TreeReplantSystem.Process();Check(Main.tile[x,y+4].TileType!=TileID.Saplings,"mid-trunk manual cut waits for remaining stump");tool.Invoke(A.Player,new object[]{A.Player.HeldItem,false,x,y+4});}
                finally {EconomySystem.Actor=oldActor;}
                TreeReplantSystem.Process();
            }
            Check(Main.tile[x,y+4].HasTile&&Main.tile[x,y+4].TileType==TileID.Saplings,"complete tree pipeline plants after stump/job finish: batch="+batch);
            int remaining=A.Player.inventory[1].stack;for(int n=0;n<20;n++){GatheringSystem.ProcessJobs();TreeReplantSystem.Process();}
            Check(Main.tile[x,y+4].TileType==TileID.Saplings&&A.Player.inventory[1].stack==remaining,"new sapling survives remaining updates and never charges twice");
        }
        Init();Check(ProgressionNetwork.ProtocolVersion==18,"new catalog and wall intent require protocol eighteen");
    }
}
