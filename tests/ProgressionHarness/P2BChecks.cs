using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using Terraria.DataStructures;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace ProgressionHarness;

// Observe the native attempt after pond, bait and eligibility evaluation.
public sealed class FishingProbe : ModPlayer
{
    internal static FishingAttempt? Last;
    public override void ModifyFishingAttempt(ref FishingAttempt attempt) => Last=attempt;
}

public sealed partial class RuntimeChecks
{
    private void RunP2B()
    {
        Reset(); A.Award(1000000 * Experience.Scale);
        var p = A.Player;
        void Toggle(string id, bool on) => A.ApplyTalent(on ? TalentOperation.Enable : TalentOperation.Disable, id, TalentCategory.Utility, 1);
        foreach (string id in new[]{"Dash","WallClimb","WallSlide","UnlimitedFlight","IceTraction","BuildingRuler","AutoPaint","FishingLine","LavaFishing"})
            Check(A.ApplyTalent(TalentOperation.Upgrade,id,TalentCategory.Utility,1)==TalentResult.Success,"P2B runtime purchase: "+id);
        p.ResetEffects(); PlayerLoader.UpdateEquips(p);
        Check(p.iceSkate && p.rulerGrid && p.rulerLine && p.autoPaint && p.accFishingLine && p.accLavaFishing,"P2B native equip flags");
        Check(!p.empressBrooch && !p.autoActuator && p.wingsLogic==0,"P2B does not grant wings or bundled accessory effects");

        int x=Main.spawnTileX+60, y=Main.spawnTileY-15;
        for(int dx=-5;dx<=5;dx++) for(int dy=-5;dy<=8;dy++) Main.tile[x+dx,y+dy].ClearEverything();
        p.position=new Vector2(x*16,y*16); p.velocity=Vector2.Zero;
        p.dash=0; p.dashType=0; p.dashDelay=0; p.dashTime=0;
        p.controlRight=true; p.releaseRight=true;
        p.DashMovement(); p.DashMovement();
        Check(p.velocity.X>10 && p.dash==1 && p.dashDelay<0,"native double-tap starts standard dash");
        Check(p.dashType==0,"dash source restored after native movement");
        Toggle("Dash",false); p.controlRight=false; p.DashMovement();
        Check(p.dashDelay!=0,"disabling dash does not reset its cooldown");
        p.dashDelay=0; p.dashTime=0; p.dashType=0; p.velocity=Vector2.Zero;
        p.controlRight=true; p.DashMovement(); p.DashMovement();
        Check(p.dash==0 && p.velocity.X==0,"disabled dash cannot start");
        Toggle("Dash",true); p.dashType=2; p.dashDelay=0; p.controlRight=false; p.DashMovement();
        Check(p.dash==2 && p.dashType==2,"real shield dash takes priority");
        p.dashType=-1; p.dashDelay=0; p.DashMovement();
        Check(p.dash==-1 && p.dashType==-1,"custom mod dash marker is preserved");
        p.dashType=0; p.dash=0; p.dashDelay=0;

        // A real solid wall supplies native collision checks, including wall-jump state.
        p.position=new Vector2(x*16,y*16); p.slideDir=1; p.controlRight=true;
        int wallX=(int)((p.position.X+p.width+1)/16);
        for(int dy=-3;dy<=8;dy++) Main.tile[wallX,y+dy].ResetToType(TileID.Stone);
        p.spikedBoots=0; p.gravity=.4f; p.gravDir=1; p.velocity.Y=5; p.controlDown=false;
        Toggle("WallClimb",false); p.WallslideMovement();
        Check(p.sliding && p.velocity.Y==.5f && p.spikedBoots==0,"slide-only native descent with scoped boots");
        Toggle("WallClimb",true); Toggle("WallSlide",false); p.velocity.Y=5; p.WallslideMovement();
        Check(p.sliding && p.velocity.Y<0,"climb independently provides native wall cling");
        p.controlJump=true; p.releaseJump=true; p.jump=0; p.JumpMovement();
        Check(p.velocity.Y<0 && p.velocity.X<0,"native wall jump pushes up and away from wall");
        p.controlJump=false; p.controlDown=true; p.velocity.Y=5; p.WallslideMovement();
        Check(p.velocity.Y==4,"native down input descends from cling");
        Toggle("WallClimb",false); p.controlDown=false; p.velocity.Y=5; p.WallslideMovement();
        Check(!p.sliding && p.velocity.Y==5,"both wall talents off restore falling");
        p.spikedBoots=2; p.velocity.Y=5; p.WallslideMovement();
        Check(p.sliding && p.spikedBoots==2,"real climbing equipment remains when talents off");
        p.spikedBoots=0; p.sliding=false; p.controlRight=false;

        p.wingsLogic=1; p.wingTimeMax=100; p.wingTime=10; p.velocity=Vector2.Zero;
        p.WingMovement(); float conservedSpeed=p.velocity.Y;
        Check(p.wingTime==10 && conservedSpeed<0,"native wing flight conserves remaining fuel");
        for(int i=0;i<600;i++) p.WingMovement();
        Check(p.wingTime==10,"flight fuel remains unchanged across 600 native ticks");
        Toggle("UnlimitedFlight",false); p.velocity=Vector2.Zero; p.WingMovement();
        Check(p.wingTime==9 && p.velocity.Y==conservedSpeed,"disabled flight resumes consumption at identical speed");
        Toggle("UnlimitedFlight",true); p.wingTime=0; PlayerLoader.UpdateEquips(p);
        Check(p.wingTime==0,"enable does not refill exhausted flight");
        p.wingTime=10; p.empressBrooch=true; p.WingMovement();
        Check(p.wingTime==100,"real soaring insignia refill is preserved");
        p.empressBrooch=false; p.wingsLogic=0;

        // Exercise the actual retrieval branch with identical deterministic RNG.
        int seed=0; while(new UnifiedRandom(seed).Next(7)!=0) seed++;
        var bobber=new Projectile { owner=0 }; bobber.localAI[1]=ItemID.Bass;
        Toggle("FishingLine",false); p.ResetEffects(); PlayerLoader.UpdateEquips(p); Main.rand=new UnifiedRandom(seed);
        Native(p,"ItemCheck_CheckFishingBobber_PullBobber",bobber,ItemID.Worm);
        Check(bobber.ai[0]==2,"native unprotected line breaks on selected roll");
        Toggle("FishingLine",true); p.ResetEffects(); PlayerLoader.UpdateEquips(p); Main.rand=new UnifiedRandom(seed);
        bobber.ai[0]=0; bobber.ai[1]=0; Native(p,"ItemCheck_CheckFishingBobber_PullBobber",bobber,ItemID.Worm);
        Check(bobber.ai[0]!=2 && bobber.ai[1]==ItemID.Bass,"purchased line protection retains catch on same roll");

        int pondX=x+30, pondY=y;
        for(int dx=-16;dx<=16;dx++) for(int dy=-1;dy<=16;dy++) {
            var tile=Main.tile[pondX+dx,pondY+dy]; tile.ClearEverything();
            if(dx==-16 || dx==16 || dy==16) tile.ResetToType(TileID.Stone);
            else if(dy>=0) { tile.LiquidType=LiquidID.Lava; tile.LiquidAmount=255; }
        }
        p.inventory[0]=new Item(ItemID.WoodFishingPole); p.inventory[1]=new Item(ItemID.MasterBait){stack=10}; p.selectedItem=0; p.wet=false;
        var lavaBobber=new Projectile { owner=0, type=ProjectileID.BobberWooden, width=14, height=14, position=new Vector2(pondX*16,pondY*16) };
        // Use the real dedicated-server identity during pond evaluation: no local
        // achievement manager exists on a dedicated server. The attempt is unchanged.
        Main.netMode=NetmodeID.Server; Main.myPlayer=255;
        Toggle("LavaFishing",false); p.ResetEffects(); PlayerLoader.UpdateEquips(p); FishingProbe.Last=null; lavaBobber.FishingCheck();
        Check(FishingProbe.Last is { inLava: true, CanFishInLava: false },"native lava attempt rejects ordinary rod and bait without ability");
        Toggle("LavaFishing",true); p.ResetEffects(); PlayerLoader.UpdateEquips(p); FishingProbe.Last=null; lavaBobber.FishingCheck();
        Check(FishingProbe.Last is { inLava: true, CanFishInLava: true },"native lava attempt accepts ordinary rod and bait with ability");
        p.inventory[1]=new Item(); FishingProbe.Last=null; lavaBobber.FishingCheck();
        Check(FishingProbe.Last==null,"lava ability does not bypass missing bait");
        Main.netMode=NetmodeID.SinglePlayer; Main.myPlayer=0;

        // Native wall placement must spend paint only when its builder toggle is on.
        p.inventory[0]=new Item(ItemID.WoodWall){stack=1}; p.selectedItem=0;
        p.inventory[1]=new Item(ItemID.RedPaint){stack=10}; p.builderAccStatus[3]=0;
        Player.tileTargetX=x; Player.tileTargetY=y;
        void PlaceWall() {
            p.itemTime=0; p.itemAnimation=20; p.controlUseItem=true;
            Native(p,"PlaceThing_Walls");
        }
        Main.tile[x,y].ClearEverything(); Main.tile[x,y+1].ResetToType(TileID.Stone);
        PlaceWall();
        Check(Main.tile[x,y].WallType==WallID.Wood && Main.tile[x,y].WallColor==p.inventory[1].paint && p.inventory[1].stack==9,"native wall placement paints and consumes exactly one paint");
        Main.tile[x,y].ClearEverything(); p.builderAccStatus[3]=1; PlaceWall();
        Check(Main.tile[x,y].WallType==WallID.Wood && Main.tile[x,y].WallColor==PaintID.None && p.inventory[1].stack==9,"native builder paint off preserves paint and still places wall");
        p.builderAccStatus[3]=0; Toggle("AutoPaint",false); p.ResetEffects(); PlayerLoader.UpdateEquips(p);
        Main.tile[x,y].ClearEverything(); PlaceWall();
        Check(Main.tile[x,y].WallColor==PaintID.None && p.inventory[1].stack==9,"disabled paint talent has no placement effect");
        Toggle("AutoPaint",true); p.inventory[1]=new Item(); p.ResetEffects(); PlayerLoader.UpdateEquips(p);
        Main.tile[x,y].ClearEverything(); PlaceWall();
        Check(Main.tile[x,y].WallType==WallID.Wood && Main.tile[x,y].WallColor==PaintID.None,"building without paint succeeds normally");

        var saved=new TagCompound(); A.SaveData(saved); B.LoadData(saved);
        Check(B.State.Talents.ContainsKey("LavaFishing") && !B.State.Talents["WallClimb"].Enabled,"P2B native save preserves independent states");
        Main.netMode=NetmodeID.Server; A.HasActionTick=false;
        SendTalentAction(A.SessionId,A.TalentRevision,TalentOperation.Disable,"LavaFishing",1);
        Check(!A.State.Talents["LavaFishing"].Enabled,"server confirms new talent toggle");
        Main.netMode=NetmodeID.MultiplayerClient;
        Check(A.ApplyTalent(TalentOperation.Enable,"LavaFishing",TalentCategory.Utility,1)==TalentResult.NotReady,"client cannot locally enable new ability");
        Main.netMode=NetmodeID.Server; var imported=StateCodec.Decode(StateCodec.Encode(A.State)); A.SessionReady=false; Receive(1,imported);
        Check(A.SessionReady && A.State.Talents.ContainsKey("Dash"),"protocol seven imports P2B catalog");
        Main.netMode=NetmodeID.SinglePlayer;
        A.ApplyTalent(TalentOperation.DisableEverything,"",TalentCategory.Utility,1); p.ResetEffects(); PlayerLoader.UpdateEquips(p);
        Check(!p.iceSkate && !p.rulerGrid && !p.autoPaint && !p.accFishingLine && !p.accLavaFishing,"all-off removes P2B flags next tick");
        p.iceSkate=true; p.rulerGrid=true; p.autoPaint=true; p.accFishingLine=true; p.accLavaFishing=true; PlayerLoader.UpdateEquips(p);
        Check(p.iceSkate && p.rulerGrid && p.autoPaint && p.accFishingLine && p.accLavaFishing,"disabled P2B preserves real equipment flags");
        Reset();
    }
}
