using System;
using System.Numerics;
using Microsoft.Xna.Framework;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Core;

namespace TerrariaProgression.Players;

public sealed class ExtendedTalentPlayer : ModPlayer
{
    internal static BigInteger Level(Player player, string id)
    {
        var p = player.GetModPlayer<ProgressionPlayer>();
        return p.SessionReady ? NumericTalents.ActiveLevel(p.State, id) : BigInteger.Zero;
    }
    private BigInteger L(string id) => Level(Player, id);
    public override void PostUpdateEquips()
    {
        // Breath maximum is player-local. Never refill when increasing it.
        Player.breathMax = TalentMath.ScaleInt(Player.breathMax, 1 + .1 * TalentMath.Level(L("Breath")));
        Player.breath = Math.Min(Player.breath, Player.breathMax);
        if (Player.whoAmI == Main.myPlayer) {
            // Tool range and placement range are independently selected by held item.
            bool tool = Player.HeldItem.pick > 0 || Player.HeldItem.axe > 0 || Player.HeldItem.hammer > 0;
            int reach = (int)BigInteger.Min(L(tool ? "ToolReach" : "BuildReach"), Math.Max(Main.maxTilesX, Main.maxTilesY));
            if (tool) { Player.tileRangeX += reach; Player.tileRangeY += reach; }
            else if (Player.HeldItem.createTile > -1 || Player.HeldItem.createWall > 0) Player.blockRange = TalentMath.AddInt(Player.blockRange, reach);
        }
    }
    public override void ModifyWeaponCrit(Item item, ref float crit)
    {
        if (item.DamageType.UseStandardCritCalcs)
            crit = (float)Math.Clamp(crit + 10 * TalentMath.Level(L("CritChance")), 0, int.MaxValue - 4096d);
    }
    internal static void ApplyCritTiers(double chance, double roll, ref NPC.HitModifiers modifiers)
    {
        if (!modifiers.DamageType.UseStandardCritCalcs || chance < 100) return;
        var tier = TalentMath.CritTier(0, chance, roll);
        modifiers.SetCrit(); // DisableCrit always wins, including calls by later mods.
        modifiers.CritDamage += TalentMath.Finite(TalentMath.Level(tier - 1));
    }
    public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
    {
        if (L("CritChance") > 0) ApplyCritTiers(Player.GetWeaponCrit(item), Main.rand.NextDouble(), ref modifiers);
    }
    public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
    {
        if (L("CritChance") > 0) ApplyCritTiers(proj.CritChance, Main.rand.NextDouble(), ref modifiers);
    }
    public override void ModifyItemScale(Item item, ref float scale)
    {
        if (!item.noMelee && item.useStyle == ItemUseStyleID.Swing && item.damage > 0)
            scale = TalentMath.Finite(scale * (1 + .1 * TalentMath.Level(L("MeleeRange"))));
    }
    public override void ModifyShootStats(Item item, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
    {
        // Velocity is assigned once at weapon firing, never multiplied every AI tick.
        double factor = 1 + .05 * TalentMath.Level(L("ProjectileSpeed"));
        velocity.X = (float)Math.Clamp(velocity.X * factor, -float.MaxValue, float.MaxValue);
        velocity.Y = (float)Math.Clamp(velocity.Y * factor, -float.MaxValue, float.MaxValue);
    }
    public override void ModifyHurt(ref Player.HurtModifiers modifiers) => modifiers.Knockback *= (float)TalentMath.Remaining(.95, L("KnockbackResistance"));
    public override void PostHurt(Player.HurtInfo info)
    {
        if (info.CooldownCounter == -1 && Player.immuneTime > 0) Player.immuneTime = TalentMath.AddInt(Player.immuneTime, L("Invulnerability"));
        else if (info.CooldownCounter >= 0 && info.CooldownCounter < Player.hurtCooldowns.Length && Player.hurtCooldowns[info.CooldownCounter] > 0)
            Player.hurtCooldowns[info.CooldownCounter] = TalentMath.AddInt(Player.hurtCooldowns[info.CooldownCounter], L("Invulnerability"));
    }
    public override void ModifyCaughtFish(Item fish)
    {
        // Native fishing is owner-client authoritative, like vanilla inventory.
        if (Player.whoAmI == Main.myPlayer && !fish.IsAir)
            Talents.EconomySystem.BoostInventoryCatch(Player, fish, L("FishingYield"));
    }
}
