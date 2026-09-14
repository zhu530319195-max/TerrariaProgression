using System;
using System.Numerics;
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
    private void RunP2Completion()
    {
        Reset(); A.Award(10000000*Experience.Scale);
        var p=A.Player; var exploration=p.GetModPlayer<ExplorationPlayer>();
        void Toggle(string id,bool on) => A.ApplyTalent(on ? TalentOperation.Enable : TalentOperation.Disable,id,TalentCategory.Utility,1);
        foreach(string id in new[]{"FlightTime","FlightSpeed","SwimSpeed","PlacementSpeed","WallPlacementSpeed"})
            A.ApplyTalent(TalentOperation.Upgrade,id,TalentCategory.Utility,10);
        foreach(string id in new[]{"NightVision","SelfLight","DangerSense"})
            A.ApplyTalent(TalentOperation.Upgrade,id,TalentCategory.Utility,1);
        p.ResetEffects(); PlayerLoader.UpdateEquips(p);
        Check(p.nightVision && p.dangerSense && exploration.EmitsLight,"exploration vision and moving light enabled independently");
        Toggle("NightVision",false); Toggle("SelfLight",false); p.ResetEffects(); PlayerLoader.UpdateEquips(p);
        Check(!p.nightVision && !exploration.EmitsLight && p.dangerSense,"vision toggles do not disable danger sense");
        p.nightVision=true; PlayerLoader.UpdateEquips(p);
        Check(p.nightVision,"disabled night vision preserves native equipment or potion flag");
        Toggle("SelfLight",true); p.dead=true;
        Check(!exploration.EmitsLight,"dead character does not emit talent light"); p.dead=false;

        void Wings() { p.wingsLogic=1; p.equippedWings=new Item(ItemID.AngelWings); p.wingTimeMax=100; }
        Wings(); p.wingTime=10; exploration.PostUpdateEquips();
        Check(p.wingTimeMax==700 && p.wingTime==10,"flight capacity gains ten seconds without airborne refill");
        p.wingTime=700; A.ApplyTalent(TalentOperation.DecreaseIntensity,"FlightTime",TalentCategory.Utility,7);
        Wings(); exploration.PostUpdateEquips();
        Check(p.wingTimeMax==280 && p.wingTime==280,"reducing flight strength clamps existing fuel to new capacity");
        Toggle("FlightTime",false); Wings(); exploration.PostUpdateEquips();
        Check(p.wingTimeMax==100 && p.wingTime==100,"disabled flight duration restores native capacity");
        Toggle("FlightTime",true); p.wingsLogic=0; p.equippedWings=null; p.wingTimeMax=0; exploration.PostUpdateEquips();
        Check(p.wingTimeMax==0,"duration cannot grant wings or standalone rocket flight");
        Wings(); p.accRunSpeed=6; p.runAcceleration=.1f; ItemLoader.HorizontalWingSpeeds(p);
        Check(Math.Abs(p.accRunSpeed-9)<.001 && Math.Abs(p.runAcceleration-.15)<.001,"native wing horizontal hook scales speed and acceleration 50 percent");
        Toggle("FlightSpeed",false); p.velocity=Vector2.Zero; p.wingTime=100; p.WingMovement(); float normalUp=p.velocity.Y;
        Toggle("FlightSpeed",true); p.velocity=Vector2.Zero; p.wingTime=100; p.WingMovement();
        Check(Math.Abs(p.velocity.Y-normalUp*1.5)<.001 && p.wingTime==99,"actual native wing ascent scales without free fuel");
        A.ApplyTalent(TalentOperation.Upgrade,"UnlimitedFlight",TalentCategory.Utility,1);
        p.velocity=Vector2.Zero; p.wingTime=10; p.WingMovement();
        Check(p.wingTime==10 && Math.Abs(p.velocity.Y-normalUp*1.5)<.001,"flight speed and unlimited fuel coexist");
        Toggle("FlightSpeed",false); p.accRunSpeed=6; p.runAcceleration=.1f; ItemLoader.HorizontalWingSpeeds(p);
        Check(p.accRunSpeed==6 && p.runAcceleration==.1f,"disabled flight speed preserves original wing inputs");
        p.wingsLogic=0; p.equippedWings=null;

        int x=Main.spawnTileX+50,y=Main.spawnTileY-20;
        for(int dx=-5;dx<40;dx++) for(int dy=-5;dy<20;dy++) Main.tile[x+dx,y+dy].ClearEverything();
        var start=new Vector2(x*16,y*16); p.position=start; p.wet=true; p.velocity=new Vector2(2,2);
        p.WaterCollision(false,false);
        Check(p.position==start+new Vector2(2,2) && p.velocity==new Vector2(2,2),"native water collision doubles level-ten movement and restores simulation velocity");
        for(int i=0;i<20;i++) p.WaterCollision(false,false);
        Check(p.position==start+new Vector2(42,42) && p.velocity==new Vector2(2,2),"swimming stays linear across repeated collision ticks");
        Toggle("SwimSpeed",false); p.position=start; p.WaterCollision(false,false);
        Check(p.position==start+Vector2.One,"disabled swimming returns native water displacement");
        Toggle("SwimSpeed",true); A.ApplyTalent(TalentOperation.DecreaseIntensity,"SwimSpeed",TalentCategory.Utility,5);
        p.position=start; p.WaterCollision(false,false);
        Check(p.position==start+new Vector2(1.5f,1.5f),"swim strength can be reduced without refund");
        A.ApplyTalent(TalentOperation.MaximumIntensity,"SwimSpeed",TalentCategory.Utility,1);
        p.position=start; p.merman=true; p.DryCollision(false,false);
        Check(p.position==start+new Vector2(4,4) && p.velocity==new Vector2(2,2),"native merman collision path receives the same swim multiplier");
        p.merman=false; p.wet=false; p.position=start; p.DryCollision(false,false);
        Check(p.position==start+new Vector2(2,2),"swimming does not accelerate dry movement");
        p.wet=true;
        foreach(string liquid in new[]{"lava","honey","shimmer"}) {
            p.lavaWet=liquid=="lava"; p.honeyWet=liquid=="honey"; p.shimmerWet=liquid=="shimmer";
            p.position=start; p.WaterCollision(false,false);
            Check(p.position==start+Vector2.One,"swim multiplier excludes "+liquid);
        }
        p.lavaWet=p.honeyWet=p.shimmerWet=false;
        p.position=start;p.velocity=new Vector2(32,0);p.canFloatInWater=true;p.controlDown=false;
        p.WaterCollision(false,false);
        Check(Math.Abs(p.velocity.Y+.4f)<.001 && p.velocity.X==32,"subdivided swimming applies native floating buoyancy exactly once");
        p.canFloatInWater=false;p.velocity=new Vector2(2,2);
        p.grapCount=1; p.position=start; p.WaterCollision(false,false);
        Check(p.position==start+Vector2.One,"swimming excludes grapple movement"); p.grapCount=0;
        for(int dy=-2;dy<6;dy++) Main.tile[x+2,y+dy].ResetToType(TileID.Stone);
        p.position=start; p.velocity=new Vector2(30,0); p.WaterCollision(false,false);
        Check(p.position.X+p.width<=(x+2)*16 && p.velocity.X<30,$"boosted water movement still stops at solid wall: right={p.position.X+p.width}, wall={(x+2)*16}, vx={p.velocity.X}");
        p.wet=false;

        // Engine action and animation timers use the same multiplier.
        int Time(Item item) { p.itemTime=0; p.ApplyItemTime(item,1,false); return p.itemTime; }
        int Animation(Item item) { p.ApplyItemAnimation(item); return p.itemAnimation; }
        var block=new Item(ItemID.DirtBlock); var wall=new Item(ItemID.WoodWall);
        p.tileSpeed=p.wallSpeed=1;
        Toggle("PlacementSpeed",false); Toggle("WallPlacementSpeed",false);
        int blockTime=Time(block),blockAnimation=Animation(block),wallTime=Time(wall);
        Toggle("PlacementSpeed",true);
        Check(Time(block)==Math.Max(1,blockTime/3) && Animation(block)==Math.Max(1,blockAnimation/3),"native tile use and animation times both become one third");
        Check(Time(wall)==wallTime,"tile placement does not speed up walls");
        Toggle("WallPlacementSpeed",true);
        Check(Time(wall)==Math.Max(1,wallTime/3),"native wall action interval becomes one third");
        var weapon=new Item(ItemID.CopperShortsword); var tool=new Item(ItemID.CopperPickaxe);
        var adapter=ModContent.GetInstance<ExplorationItem>();
        Check(adapter.UseSpeedMultiplier(weapon,p)==1 && adapter.UseSpeedMultiplier(tool,p)==1,"placement speed excludes weapon and mining metadata");
        p.tileSpeed=.5f;
        Check(Time(block)==Math.Max(1,blockTime/3),"explicit timer multiplier remains caller-owned");
        Check(Animation(block)==Math.Max(1,(int)(blockAnimation*.5/3)),"native building equipment animation multiplier stacks once");
        p.tileSpeed=1;
        // Actual wall placement still follows native placement validation.
        p.position=start; p.inventory[0]=new Item(ItemID.WoodWall){stack=10};p.selectedItem=0;
        Player.tileTargetX=x; Player.tileTargetY=y;
        Main.tile[x,y].ClearEverything(); Main.tile[x,y+1].ResetToType(TileID.Stone);
        p.itemTime=0;p.itemAnimation=20;p.controlUseItem=true;
        Native(p,"PlaceThing_Walls");
        Check(Main.tile[x,y].WallType==WallID.Wood && p.itemTime==Math.Max(1,wallTime/3),"real wall placement uses accelerated native timer");
        var saved=new TagCompound(); A.SaveData(saved); B.LoadData(saved);
        Check(NumericTalents.ActiveLevel(B.State,"FlightTime")==3 && !B.State.Talents["NightVision"].Enabled,"native save retains completion intensity and independent toggles");
        Main.netMode=NetmodeID.Server;A.HasActionTick=false;
        SendTalentAction(A.SessionId,A.TalentRevision,TalentOperation.Disable,"SwimSpeed",1);
        Check(!A.State.Talents["SwimSpeed"].Enabled,"server confirms completion talent toggle");
        var imported=StateCodec.Decode(StateCodec.Encode(A.State));A.SessionReady=false;Receive(1,imported);
        Check(A.SessionReady && A.State.Talents.ContainsKey("FlightTime") && TerrariaProgression.Networking.ProgressionNetwork.ProtocolVersion==15,"current protocol imports completion catalog");
        Reset();
    }
}

