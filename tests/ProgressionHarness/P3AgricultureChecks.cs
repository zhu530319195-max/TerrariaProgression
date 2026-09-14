using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ObjectData;
using TerrariaProgression.Core;
using TerrariaProgression.Config;
using TerrariaProgression.Players;
using TerrariaProgression.Talents;
using TerrariaProgression.Networking;

namespace ProgressionHarness;
public sealed partial class RuntimeChecks
{
    private void RunP3Agriculture()
    {
        int x=Main.spawnTileX+40,y=Main.spawnTileY-15;
        uint seq=0;
        void Advance(ulong ticks=100)=>typeof(Main).GetField("_gameUpdateCount",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,unchecked(Main.GameUpdateCount+(uint)ticks));
        void Init() {
            Reset();GatheringSystem.Clear();seq=0;A.Award(100000000*Experience.Scale);
            Config.EnableWorldGathering=Config.EnableAreaHarvest=Config.EnableAutoReplant=true;
            Config.MaxBlocksPerAction=1000;Config.GatheringWorkPerTick=32;Config.ProtectedTileAreas.Clear();
            A.Player.inventory[0]=new Item(ItemID.CopperPickaxe);A.Player.selectedItem=0;A.Player.pickSpeed=1;
            A.Player.Center=new Vector2(x*16,y*16);B.Player.Center=A.Player.Center+new Vector2(300,0);
            A.Player.GetModPlayer<GatheringPlayer>().BatchEnabled=true;
            Main.dayTime=true;Main.time=10000;Main.raining=false;Main.cloudAlpha=0;Main.bloodMoon=false;Main.moonPhase=4;
            for(int dx=-14;dx<=14;dx++)for(int dy=-14;dy<=14;dy++)Main.tile[x+dx,y+dy].ClearEverything();
            foreach(var item in Main.item)item.active=false;Array.Clear(Main.timeItemSlotCannotBeReusedFor);
        }
        void Buy(string id,int n=1)=>Check(A.ApplyTalent(TalentOperation.Upgrade,id,TalentCategory.Utility,n)==TalentResult.Success,"agriculture purchase: "+id);
        void Put(int dx,int dy,ushort type) {var t=Main.tile[x+dx,y+dy];t.ClearEverything();t.HasTile=true;t.TileType=type;}
        void Herb(int dx,int dy,int style,ushort stage=TileID.BloomingHerbs,ushort soil=TileID.PlanterBox) {
            Put(dx,dy+2,TileID.Stone);Put(dx,dy+1,soil);Put(dx,dy,stage);var t=Main.tile[x+dx,y+dy];t.TileFrameX=(short)(18*style);
        }
        bool Has(int dx,int dy)=>Main.tile[x+dx,y+dy].HasTile;
        bool Seedling(int dx,int dy,int style)=>Has(dx,dy)&&Main.tile[x+dx,y+dy].TileType==TileID.ImmatureHerbs&&Main.tile[x+dx,y+dy].TileFrameX==style*18;
        int Items(int type)=>Main.item.Take(Main.maxItems).Where(i=>i.active&&i.type==type).Sum(i=>i.stack);
        bool Request(GatheringMode mode=GatheringMode.Harvest,int dx=0,int dy=0) {
            Advance();return GatheringSystem.Request(A.Player,mode,x+dx,y+dy,A.SessionId,A.TalentRevision,++seq,0,A.Player.HeldItem.type);
        }
        void Drain() {for(int i=0;i<3000&&GatheringSystem.PendingCount>0;i++){Advance(1);GatheringSystem.ProcessJobs();}Check(GatheringSystem.PendingCount==0,"agriculture queue terminates");}
        Init();Buy("AreaHarvest");Herb(0,0,0);Herb(1,0,1);Herb(-1,0,2,TileID.ImmatureHerbs);Herb(2,0,0);
        Check(Request(),"harvest request accepted");Drain();
        Check(!Has(0,0)&&!Has(1,0)&&Has(-1,0)&&Has(2,0)&&Has(0,1)&&Has(1,1),"3x3 collects mixed ready herbs and preserves seedlings, exterior and planters");
        Check(Items(ItemID.Daybloom)==1&&Items(ItemID.Moonglow)==1,"area creates native herbs exactly once");
        Init();Buy("AreaHarvest",10);Herb(0,0,0);Herb(10,10,0);Herb(11,0,0);Request();Drain();
        Check(!Has(10,10)&&Has(11,0),"Lv10 reaches 21x21 corner, not outside");
        Init();Buy("AreaHarvest",10);A.ApplyTalent(TalentOperation.DecreaseIntensity,"AreaHarvest",TalentCategory.Utility,9);Herb(0,0,0);Herb(2,0,0);Request();Drain();
        Check(Has(2,0)&&A.State.Talents["AreaHarvest"].TalentLevel==10,"harvest intensity uses selected radius without reducing paid level");
        // Exact native flowering conditions are the eligibility authority.
        foreach(int style in Enumerable.Range(0,7)) {
            Init();Buy("AreaHarvest");Herb(0,0,style,TileID.MatureHerbs);
            bool ready=WorldGen.IsHarvestableHerbWithSeed(TileID.MatureHerbs,style);
            Check(Request()==ready,"mature-stage eligibility agrees with native seed rule: "+style);
            Check(Has(0,0)!=ready,"non-seeding mature plants remain untouched: "+style);
        }
        // All seven species on native ground, planter boxes and real clay pots.
        ushort[] soils={TileID.Grass,TileID.JungleGrass,TileID.Dirt,TileID.CorruptGrass,TileID.Sand,TileID.Ash,TileID.SnowBlock};
        for(int style=0;style<7;style++)foreach(int container in new[]{0,1,2}) {
            Init();Buy("AreaHarvest");Buy("AutoReplant");Buy("HerbYield",10);
            if(container==2) {
                var pot = TileObjectData.GetTileData(TileID.ClayPot, 0);
                for (int dx=0;dx<pot.Width;dx++) Put(dx,1+pot.Height,TileID.Stone);
                WorldGen.PlaceTile(x+pot.Origin.X,y+1+pot.Origin.Y,TileID.ClayPot,forced:false);
                Check(Has(0,1)&&Main.tile[x,y+1].TileType==TileID.ClayPot,"native clay pot fixture placed");
                Put(0,0,TileID.BloomingHerbs);var t=Main.tile[x,y];t.TileFrameX=(short)(style*18);
            } else Herb(0,0,style,TileID.BloomingHerbs,container==0?soils[style]:TileID.PlanterBox);
            int seed=AgricultureSystem.Seed(style);A.Player.inventory[1]=new Item(seed,5);
            Check(Request(),"native supported harvest: species/container "+style+"/"+container);Drain();
            Check(Seedling(0,0,style)&&Has(0,1),"same species replanted preserving support: "+style+"/"+container+", tile="+Main.tile[x,y].TileType+", active="+Has(0,0)+", support="+Has(0,1));
            Check(A.Player.inventory[1].stack==5,"newly harvested seed is preferred to inventory");
            int remaining=Items(seed);Check(remaining>=1&&remaining<=5&&remaining%2==1,"native 1-3 seeds doubled once, then exactly one consumed");
            int herb=style==6?ItemID.Shiverthorn:ItemID.Daybloom+style;
            Check(Items(herb)==2,"herb yield doubles native output exactly once");
        }
        Init();Buy("AutoReplant");Main.dayTime=false;Herb(0,0,0,TileID.MatureHerbs);A.Player.GetModPlayer<GatheringPlayer>().BatchEnabled=false;
        Check(Request(GatheringMode.SingleHerb)&&!Has(0,0)&&Items(ItemID.Daybloom)==1,"single harvest without seeds succeeds with batch off and no area talent");
        Init();Buy("AutoReplant");Main.dayTime=false;Herb(0,0,0,TileID.MatureHerbs);A.Player.inventory[49]=new Item(ItemID.DaybloomSeeds,2);
        Check(Request(GatheringMode.SingleHerb)&&Seedling(0,0,0)&&A.Player.inventory[49].stack==1,"single non-flowering herb replants from main inventory");
        Init();Buy("AutoReplant");Main.dayTime=false;Herb(0,0,0,TileID.MatureHerbs);
        A.Player.inventory[1]=new Item(ItemID.MoonglowSeeds,5);
        int old=Item.NewItem(A.Player.GetSource_Misc("old seed"),A.Player.Hitbox,ItemID.DaybloomSeeds,20);
        Request(GatheringMode.SingleHerb);
        Check(!Has(0,0)&&Main.item[old].stack==20&&A.Player.inventory[1].stack==5,"does not consume unrelated ground stacks or wrong-species seeds");
        Init();Buy("AreaHarvest");Buy("AutoReplant");Herb(0,0,0,soil:TileID.Stone);A.Player.inventory[1]=new Item(ItemID.DaybloomSeeds,3);
        Request();Drain();Check(!Has(0,0)&&A.Player.inventory[1].stack==3&&Items(ItemID.DaybloomSeeds)>=1,"failed native placement keeps every seed and harvested herb");
        Init();Buy("AreaHarvest");Buy("AutoReplant");Herb(0,0,0);Config.EnableAutoReplant=false;Request();Drain();
        Check(!Has(0,0)&&Items(ItemID.DaybloomSeeds)>=1,"independent server replant switch preserves harvesting");
        Init();Buy("AreaHarvest");Herb(0,0,0);Config.ProtectedTileAreas.Add(new ProtectedTileArea{X=x,Y=y,Width=1,Height=1});
        Check(!Request()&&Has(0,0),"protected region blocks first herb");Config.ProtectedTileAreas.Clear();var wired=Main.tile[x,y];wired.RedWire=true;
        Check(!Request()&&Has(0,0),"wired herb remains protected");wired.RedWire=false;Config.EnableWorldGathering=false;
        Check(!Request()&&Has(0,0),"server master blocks agricultural actions");
        Init();Buy("AreaHarvest");Herb(0,0,0);Herb(1,0,0);Herb(-1,0,0);Config.MaxBlocksPerAction=2;Config.GatheringWorkPerTick=1;
        Request();GatheringSystem.ProcessJobs();Check(Items(ItemID.Daybloom)==1,"one-work scan budget defers neighbouring harvests");Drain();Check(Items(ItemID.Daybloom)==2,"action cap counts harvested plants including origin");
        Init();Buy("AreaHarvest");Herb(0,0,0);Herb(1,0,0);Request();
        Check(GatheringSystem.SetBatch(A.Player,A.SessionId,++seq,false)&&GatheringSystem.PendingCount==0&&Has(1,0),"toggle off immediately cancels queued work");
        Check(!Request()&&Has(1,0),"batch requests are rejected while off");
        Check(!GatheringSystem.SetBatch(A.Player,A.SessionId,1,true),"replayed toggle cannot re-enable batch");
        Check(!GatheringSystem.SetBatch(A.Player,Guid.NewGuid(),++seq,true),"wrong-session toggle rejected");
        var gp=A.Player.GetModPlayer<GatheringPlayer>();gp.BatchEnabled=true;gp.OnEnterWorld();Check(!gp.BatchEnabled&&gp.Sequence==0,"entering world resets batch and request sequence");
        gp.BatchEnabled=true;var tag=new TagCompound();gp.SaveData(tag);Check(!tag.ContainsKey("BatchEnabled"),"batch activation is not saved");
        // Actual managed input entry, rather than direct Harvest calls.
        Init();Buy("AreaHarvest");Buy("AutoReplant");Herb(0,0,0);Main.gameMenu=false;Main.drawingPlayerChat=false;A.Player.mouseInterface=false;
        var tool=typeof(Player).GetMethod("ItemCheck_UseMiningTools_ActuallyUseMiningTool",BindingFlags.Instance|BindingFlags.NonPublic)!;
        tool.Invoke(A.Player,new object[]{A.Player.HeldItem,false,x,y});Drain();Check(Seedling(0,0,0),"ordinary pick input routes through agriculture");
        Init();Buy("AutoReplant");Herb(0,0,0);A.Player.GetModPlayer<GatheringPlayer>().BatchEnabled=false;Config.EnableAutoReplant=false;
        tool.Invoke(A.Player,new object[]{A.Player.HeldItem,false,x,y});
        Check(!Has(0,0)&&Items(ItemID.Daybloom)==1,"server-disabled replant preserves ordinary manual harvesting");
        Init();Buy("AreaMining");Herb(0,0,0);
        tool.Invoke(A.Player,new object[]{A.Player.HeldItem,false,x,y});
        Check(!Has(0,0),"batch mining without agriculture talents preserves ordinary single-herb input");
        // Regression: batch ON must not swallow explicit single-plant input.
        foreach (bool replant in new[] { false, true }) {
            Init();Buy("AreaHarvest");if(replant)Buy("AutoReplant");
            Main.dayTime=false;Herb(0,0,0,TileID.MatureHerbs);Herb(1,0,0,TileID.BloomingHerbs);
            A.Player.inventory[1]=new Item(ItemID.DaybloomSeeds,2);
            var oldActor=EconomySystem.Actor;EconomySystem.Actor=A.Player;
            try {tool.Invoke(A.Player,new object[]{A.Player.HeldItem,false,x,y});}
            finally {EconomySystem.Actor=oldActor;}
            Check((replant?Seedling(0,0,0):!Has(0,0)) && Items(ItemID.Daybloom)==1
                && Main.tile[x+1,y].TileType==TileID.BloomingHerbs && GatheringSystem.PendingCount==0,
                "batch ON non-seeding mature target uses single harvest without starting area: replant="+replant);
            Check(A.Player.inventory[1].stack==(replant?1:2),"single fallback consumes seeds only for successful replant");
        }
        Init();Buy("AreaHarvest");Herb(0,0,0);Config.EnableAreaHarvest=false;
        var prior=EconomySystem.Actor;EconomySystem.Actor=A.Player;
        try {tool.Invoke(A.Player,new object[]{A.Player.HeldItem,false,x,y});}
        finally {EconomySystem.Actor=prior;}
        Check(!Has(0,0)&&Items(ItemID.Daybloom)==1,"disabled area permission does not swallow manual herb pick");
        Init();Buy("AreaHarvest");Buy("AutoReplant");Herb(0,0,0);Herb(1,0,0,TileID.ImmatureHerbs);Config.EnableWorldGathering=false;
        prior=EconomySystem.Actor;EconomySystem.Actor=A.Player;
        try {tool.Invoke(A.Player,new object[]{A.Player.HeldItem,false,x,y});}
        finally {EconomySystem.Actor=prior;}
        Check(!Has(0,0)&&Has(1,0),"master off preserves native targeted picking without auto replant");
        Init();Buy("AreaHarvest");Herb(0,0,0);var targetWire=Main.tile[x,y];targetWire.RedWire=true;
        prior=EconomySystem.Actor;EconomySystem.Actor=A.Player;
        try {tool.Invoke(A.Player,new object[]{A.Player.HeldItem,false,x,y});}
        finally {EconomySystem.Actor=prior;}
        Check(!Has(0,0)&&GatheringSystem.PendingCount==0,"batch-protected herb remains manually targetable without an area request");
        // Nearby regrowth staff must not be mistaken for the harvesting player.
        Init();Buy("AutoReplant");Herb(0,0,0,TileID.MatureHerbs);Main.dayTime=false;
        B.Player.Center=new Vector2(x*16,y*16);A.Player.Center=B.Player.Center-new Vector2(40,0);B.Player.inventory[0]=new Item(ItemID.StaffofRegrowth);
        Request(GatheringMode.SingleHerb);Check(!Has(0,0)&&Items(ItemID.DaybloomSeeds)==0,"native herb drops use actual operator, not nearby staff owner");
        // Decoder covers authoritative toggle + harvest and replay rejection.
        Init();Buy("AreaHarvest");Buy("AutoReplant");Herb(0,0,0);Herb(1,0,0);Main.netMode=NetmodeID.Server;gp=A.Player.GetModPlayer<GatheringPlayer>();gp.BatchEnabled=false;
        void Packet(bool toggle,uint number,bool enabled=true,Guid? session=null) {
            using var bytes=new MemoryStream();using var w=new BinaryWriter(bytes);
            w.Write(ProgressionNetwork.ProtocolVersion);w.Write((byte)(toggle?ProgressionMessage.GatheringToggle:ProgressionMessage.GatheringAction));
            w.Write((session??A.SessionId).ToByteArray());
            if(toggle){w.Write(number);w.Write(enabled);}else{w.Write(A.TalentRevision);w.Write(number);w.Write((byte)GatheringMode.Harvest);w.Write(x);w.Write(y);w.Write(0);w.Write(A.Player.HeldItem.type);}
            w.Flush();bytes.Position=0;ProgressionNetwork.Receive(new BinaryReader(bytes),0);
        }
        Packet(true,1);Advance();Packet(false,2);Check(Seedling(0,0,0)&&GatheringSystem.PendingCount==1,"server owns herb removal, seeds and replacement");
        Packet(true,3,false);Check(!gp.BatchEnabled&&GatheringSystem.PendingCount==0&&Has(1,0),"server toggle packet cancels remaining harvest");
        Packet(true,1);Check(!gp.BatchEnabled,"server rejects reordered enable packet");
        Packet(true,4,true,Guid.NewGuid());Check(!gp.BatchEnabled,"server rejects stale session toggle packet");
        Main.netMode=NetmodeID.MultiplayerClient;Herb(0,0,0);Check(!Request()&&Main.tile[x,y].TileType==TileID.BloomingHerbs,"client cannot directly harvest or award seeds");
        GatheringSystem.Clear();Config.ProtectedTileAreas.Clear();Config.MaxBlocksPerAction=1000;Config.GatheringWorkPerTick=32;
        Config.EnableWorldGathering=Config.EnableAreaHarvest=Config.EnableAutoReplant=true;Main.netMode=NetmodeID.SinglePlayer;
    }
}
