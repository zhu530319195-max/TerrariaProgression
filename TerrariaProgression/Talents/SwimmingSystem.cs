using System;
using Terraria;
using Terraria.ModLoader;
using TerrariaProgression.Players;

namespace TerrariaProgression.Talents;

// Scale only the native collision movement, then restore simulation velocity.
// No per-frame multiplicative acceleration and no direct extra teleport step.
public sealed class SwimmingSystem : ModSystem
{
    private sealed class SwimScope(Player player) {
        internal readonly Player Player=player;
        internal On_Player.orig_TryFloatingInFluid? Float;
    }
    private static SwimScope? scope;
    public override void Load() { On_Player.WaterCollision+=Water; On_Player.DryCollision+=Dry; On_Player.TryFloatingInFluid+=Float; }
    public override void Unload() { On_Player.WaterCollision-=Water; On_Player.DryCollision-=Dry; On_Player.TryFloatingInFluid-=Float; scope=null; }
    private static float Factor(Player p)
    {
        if (!p.wet || p.lavaWet || p.honeyWet || p.shimmerWet || p.shimmering || p.dead || p.mount.Active ||
            p.pulley || p.grapCount>0 || p.dashDelay<0) return 1;
        double speed=Math.Max(Math.Abs(p.velocity.X),Math.Abs(p.velocity.Y));
        if (!double.IsFinite(speed) || speed<=0 || speed>=256) return 1;
        return (float)Math.Min(256/speed,ExplorationPlayer.Factor(p,"SwimSpeed",.1));
    }
    private static void Water(On_Player.orig_WaterCollision orig, Player p, bool fallThrough, bool ignorePlats)
    {
        float factor=Factor(p);
        if (factor<=1) { orig(p,fallThrough,ignorePlats); return; }
        // Native WaterCollision lacks DryCollision's high-speed stepping.
        // At most 32 calls (8px steps), independent of the talent's stored level.
        int steps=Math.Max(1,(int)Math.Ceiling(Math.Max(Math.Abs(p.velocity.X),Math.Abs(p.velocity.Y))*factor/8));
        float scale=factor/steps;
        var previous=scope; var current=new SwimScope(p); scope=current;
        p.velocity*=scale;
        try { for(int i=0;i<steps;i++) orig(p,fallThrough,ignorePlats); }
        finally { p.velocity/=scale; scope=previous; }
        // Floating tube buoyancy runs once, with restored simulation velocity.
        current.Float?.Invoke(p);
    }
    private static void Float(On_Player.orig_TryFloatingInFluid orig, Player p)
    {
        if (scope?.Player==p) scope.Float=orig;
        else orig(p);
    }
    private static void Dry(On_Player.orig_DryCollision orig, Player p, bool fallThrough, bool ignorePlats)
    {
        // Neptune shell / ignoreWater / trident use this native path even in water.
        float factor=Factor(p); p.velocity*=factor;
        try { orig(p,fallThrough,ignorePlats); } finally { p.velocity/=factor; }
    }
}
