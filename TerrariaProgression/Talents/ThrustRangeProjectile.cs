using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace TerrariaProgression.Talents;

public sealed class ThrustRangeProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    private float applied = 1;
    private Vector2 nativeCenter, anchor;
    private bool transformed;
    internal static bool Supported(Projectile p) => p.type > ProjectileID.None && p.type < ProjectileID.Count &&
        p.aiStyle is ProjAIStyleID.Spear or ProjAIStyleID.ShortSword && p.friendly && p.owner >= 0 && p.owner < Main.maxPlayers && p.CountsAsClass(DamageClass.Melee);
    public override bool PreAI(Projectile p)
    {
        // Undo only our previous transform. Vanilla computes the next thrust from
        // its own coordinates; neither AI ticks nor repeated uses compound scale.
        if (transformed) { p.Center = nativeCenter; p.scale /= applied; transformed = false; }
        applied = 1;
        return true;
    }
    public override void PostAI(Projectile p)
    {
        if (!Supported(p) || !p.active || !Main.player[p.owner].active) return;
        var player = Main.player[p.owner];
        var level = ExtendedTalentPlayer.Level(player, "MeleeRange");
        if (level <= 0) return;
        // Native coordinates and Rectangle use bounded numbers. No level cap.
        applied = (float)Math.Min(1 + .1 * TalentMath.Level(level), Math.Max(Main.maxTilesX, Main.maxTilesY));
        anchor = player.RotatedRelativePoint(player.MountedCenter, false, p.aiStyle != ProjAIStyleID.ShortSword);
        nativeCenter = p.Center;
        p.Center = anchor + (nativeCenter - anchor) * applied;
        p.scale *= applied;
        transformed = true;
    }
    public override void ModifyDamageHitbox(Projectile p, ref Rectangle hitbox)
    {
        if (transformed) hitbox = ScaledAround(hitbox, p.Center, applied);
    }
    public override bool? Colliding(Projectile p, Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (!transformed) return null;
        if (ScaledAround(p.Hitbox, p.Center, applied).Intersects(targetHitbox)) return true;
        if (p.aiStyle != ProjAIStyleID.Spear) return false;
        var displayed = p.Center;
        try {
            p.Center = nativeCenter;
            // Preserve the original spear extension hitbox and its timing, then
            // transform that box with the same anchor as the visible weapon.
            return p.AI_019_Spears_GetExtensionHitbox(out var extension) && ScaledAround(extension, anchor, applied).Intersects(targetHitbox);
        }
        finally { p.Center = displayed; }
    }
    internal static Rectangle ScaledAround(Rectangle box, Vector2 origin, float factor)
    {
        double left = origin.X + (box.Left - origin.X) * (double)factor;
        double top = origin.Y + (box.Top - origin.Y) * (double)factor;
        return new Rectangle((int)Math.Clamp(Math.Floor(left), -100000000, 100000000),
            (int)Math.Clamp(Math.Floor(top), -100000000, 100000000),
            (int)Math.Clamp(Math.Ceiling(box.Width * (double)factor), 1, 100000000),
            (int)Math.Clamp(Math.Ceiling(box.Height * (double)factor), 1, 100000000));
    }
}
