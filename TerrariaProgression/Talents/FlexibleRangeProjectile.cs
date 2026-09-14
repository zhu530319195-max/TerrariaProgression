using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace TerrariaProgression.Talents;

// Use the native geometry/AI parameters; secondary projectiles are not enlarged.
public sealed class FlexibleRangeProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    private float flailFactor = 1;
    internal static bool OwnerReady(Projectile p) => p.owner >= 0 && p.owner < Main.maxPlayers && Main.player[p.owner].active;
    internal static float Factor(Projectile p) => OwnerReady(p)
        ? (float)Math.Clamp(1 + .1 * TalentMath.Level(ExtendedTalentPlayer.Level(Main.player[p.owner], "MeleeRange")), 1, Math.Max(1, Math.Max(Main.maxTilesX, Main.maxTilesY))) : 1;
    internal static bool Flail(Projectile p) => p.type > ProjectileID.None && p.type < ProjectileID.Count && p.aiStyle == ProjAIStyleID.Flail && OwnerReady(p);
    public override void FlailStats(Projectile p, ref int launchTimeLimit, ref float launchSpeed, ref float maxLaunchLength,
        ref float retractAcceleration, ref float maxRetractSpeed, ref float forcedRetractAcceleration,
        ref float maxForcedRetractSpeed, ref int ricochetTimeLimit, ref float spinVisualDistance)
    {
        if (!Flail(p)) return;
        float speed = Math.Max(.01f, Main.player[p.owner].GetAttackSpeed(DamageClass.Melee));
        // Native AI kills a flail more than 900 px from its owner. Saturate only
        // the engine output, leaving levels and paid costs intact.
        float f = flailFactor = Math.Max(1, Math.Min(Factor(p), 800 / Math.Max(spinVisualDistance, launchSpeed * (launchTimeLimit + 2) * speed)));
        // Longer throw at native timing; matching return speed avoids prolonged lockout.
        launchSpeed *= f; maxLaunchLength *= f; spinVisualDistance *= f;
        retractAcceleration *= f; maxRetractSpeed *= f; forcedRetractAcceleration *= f; maxForcedRetractSpeed *= f;
    }
    public override void FlailSpinCollisionRange(Projectile p, ref float range) { if (Flail(p)) range *= flailFactor; }
}

public sealed class WhipRangeSystem : ModSystem
{
    public override void Load() => On_Projectile.GetWhipSettings += Settings;
    public override void Unload() => On_Projectile.GetWhipSettings -= Settings;
    private static void Settings(On_Projectile.orig_GetWhipSettings orig, Projectile p, out float time, out int segments, out float range)
    {
        orig(p, out time, out segments, out range);
        if (p.type > ProjectileID.None && p.type < ProjectileID.Count && ProjectileID.Sets.IsAWhip[p.type])
            range *= FlexibleRangeProjectile.Factor(p);
        // Both drawing and native collision obtain the same control points from this.
    }
}
