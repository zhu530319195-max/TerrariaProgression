using System;
using System.Numerics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Core;

namespace TerrariaProgression.Players;

public sealed class NumericTalentPlayer : ModPlayer
{
    private double fixedManaCarry, naturalManaCarry;
    private BigInteger L(string id)
    {
        var progression = Player.GetModPlayer<ProgressionPlayer>();
        return Main.netMode != NetmodeID.SinglePlayer && !progression.SessionReady
            ? BigInteger.Zero : NumericTalents.ActiveLevel(progression.State, id);
    }
    private double N(string id) => TalentMath.Level(L(id));
    public override void ModifyMaxStats(out StatModifier health, out StatModifier mana)
    {
        health = mana = StatModifier.Default;
        health.Base = (float)Math.Min(25 * N("MaxLife"), int.MaxValue - 1024d);
        mana.Base = (float)Math.Min(20 * N("MaxMana"), int.MaxValue - 1024d);
    }
    public override void PostUpdateEquips()
    {
        Player.statDefense += TalentMath.AddInt(0, 4 * L("Defense"));
        Player.GetDamage(DamageClass.Generic) += TalentMath.Finite(.05 * N("Damage"));
        Player.GetAttackSpeed(DamageClass.Generic) += TalentMath.Finite(.03 * N("AttackSpeed"));
        Player.GetArmorPenetration(DamageClass.Generic) += TalentMath.Finite(3 * N("ArmorPenetration"));
        Player.GetKnockback(DamageClass.Generic) += TalentMath.Finite(.1 * N("Knockback"));
        Player.manaCost *= (float)TalentMath.Remaining(.95, L("ManaSaving"));
        Player.maxMinions = TalentMath.AddInt(Player.maxMinions, L("Minions"));
        Player.maxTurrets = TalentMath.AddInt(Player.maxTurrets, L("Sentries"));
    }
    public override void PostUpdateRunSpeeds()
    {
        // Apply at the documented speed hook so running boots don't erase the bonus.
        double speed = 1 + .05 * N("MoveSpeed");
        Player.maxRunSpeed = TalentMath.Finite(Player.maxRunSpeed * speed);
        Player.accRunSpeed = TalentMath.Finite(Player.accRunSpeed * speed);
        Player.runAcceleration = TalentMath.Finite(Player.runAcceleration * (1 + .05 * N("Acceleration")));
    }
    public override void UpdateLifeRegen()
    {
        if (L("LifeRegen") > 0)
            Player.lifeRegen = (int)BigInteger.Clamp(Player.lifeRegen + 2 * L("LifeRegen"), int.MinValue, int.MaxValue);
    }
    public override void NaturalLifeRegen(ref float regen) => regen = TalentMath.Finite(regen * (1 + .1 * N("NaturalLifeRegen")));
    public override void GetHealLife(Item item, bool quickHeal, ref int healValue) => healValue = TalentMath.ScaleInt(healValue, 1 + .05 * N("Healing"));
    public override void GetHealMana(Item item, bool quickHeal, ref int healValue) => healValue = TalentMath.ScaleInt(healValue, 1 + .05 * N("ManaRestoration"));
    public override bool CanConsumeAmmo(Item weapon, Item ammo) => L("AmmoSaving") == 0 || Main.rand.NextDouble() < TalentMath.Remaining(.93, L("AmmoSaving"));
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => modifiers.CritDamage += TalentMath.Finite(.05 * N("CritDamage"));
    public override void PostUpdateMiscEffects() => ClampResources();
    internal void ClampResources()
    {
        // Increasing max stats never adds current life/mana. Lowering them only clips.
        Player.statLife = Math.Min(Player.statLife, Player.statLifeMax2);
        Player.statMana = Math.Min(Player.statMana, Player.statManaMax2);
    }
    public override void PostUpdate()
    {
        if (Player.dead || Player.statLife <= 0) { ClearCarry(); return; }
        // Vanilla final manaRegen is measured in 1/120 MP per tick (1/2 MP/s).
        // Supplement only its computed positive rate, preserving its delay, movement,
        // mana-sickness and buff decisions. Fixed mana is a separate MP/s contribution.
        // Normal Terraria owner-client resource sync is used, as for vanilla recovery.
        if (Player.whoAmI != Main.myPlayer) return;
        Player.statMana = TalentMath.Recover(Player.statMana, Player.statManaMax2,
            Math.Max(0, Player.manaRegen) * .05 * N("NaturalManaRegen"), ref naturalManaCarry);
        Player.statMana = TalentMath.Recover(Player.statMana, Player.statManaMax2,
            2 * N("ManaRegen"), ref fixedManaCarry);
    }
    public override void UpdateDead() => ClearCarry();
    public override void OnEnterWorld() => ClearCarry();
    private void ClearCarry() => fixedManaCarry = naturalManaCarry = 0;
}
