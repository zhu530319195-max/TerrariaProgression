using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TerrariaProgression.Core;

public enum TalentCategory : byte { BaseStats, Recovery, Combat, Economy, Utility, World }
public enum TalentOperation : byte { Upgrade, RefundOne, RefundTalent, Enable, Disable, RefundCategory, RefundEverything, EnableEverything, DisableEverything, DecreaseIntensity, IncreaseIntensity, MaximumIntensity, ToggleChild }
public enum TalentResult : byte { Success, NotReady, UnknownTalent, InvalidRequest, NotEnoughPoints, NoChange, StaleRequest, Capacity }
public enum EffectUnit { Flat, Percent, PerSecond, RemainingMultiplier, Flag, Multiplier, Seconds }
public sealed record TalentDefinition(string Id, TalentCategory Category, decimal PerLevel, EffectUnit Unit, bool Adjustable = false, int DefaultCost = 1, int MaxLevel = 0);

// Only implemented and testable entries are exposed to players. P2 will have its
// own explicit FunctionalTalentRegistry and can share the generic save ledger.
public static class NumericTalents
{
    public static readonly IReadOnlyList<TalentDefinition> All = Array.AsReadOnly(new[] {
        new TalentDefinition("MaxLife", TalentCategory.BaseStats, 25, EffectUnit.Flat),
        new TalentDefinition("MaxMana", TalentCategory.BaseStats, 20, EffectUnit.Flat),
        new TalentDefinition("Defense", TalentCategory.BaseStats, 4, EffectUnit.Flat),
        new TalentDefinition("MoveSpeed", TalentCategory.BaseStats, 5, EffectUnit.Percent),
        new TalentDefinition("Acceleration", TalentCategory.BaseStats, 5, EffectUnit.Percent),
        new TalentDefinition("LifeRegen", TalentCategory.Recovery, 1, EffectUnit.PerSecond),
        new TalentDefinition("NaturalLifeRegen", TalentCategory.Recovery, 10, EffectUnit.Percent),
        new TalentDefinition("ManaRegen", TalentCategory.Recovery, 2, EffectUnit.PerSecond),
        new TalentDefinition("NaturalManaRegen", TalentCategory.Recovery, 10, EffectUnit.Percent),
        new TalentDefinition("Healing", TalentCategory.Recovery, 5, EffectUnit.Percent),
        new TalentDefinition("ManaRestoration", TalentCategory.Recovery, 5, EffectUnit.Percent),
        new TalentDefinition("Damage", TalentCategory.Combat, 5, EffectUnit.Percent),
        new TalentDefinition("AttackSpeed", TalentCategory.Combat, 3, EffectUnit.Percent),
        new TalentDefinition("CritDamage", TalentCategory.Combat, 5, EffectUnit.Percent),
        new TalentDefinition("ArmorPenetration", TalentCategory.Combat, 3, EffectUnit.Flat),
        new TalentDefinition("Knockback", TalentCategory.Combat, 10, EffectUnit.Percent),
        new TalentDefinition("AmmoSaving", TalentCategory.Combat, .93m, EffectUnit.RemainingMultiplier),
        new TalentDefinition("ManaSaving", TalentCategory.Combat, .95m, EffectUnit.RemainingMultiplier),
        new TalentDefinition("Minions", TalentCategory.Combat, 1, EffectUnit.Flat),
        new TalentDefinition("Sentries", TalentCategory.Combat, 1, EffectUnit.Flat),
        new TalentDefinition("PickupRange", TalentCategory.Economy, 10, EffectUnit.Percent),
        new TalentDefinition("JumpSpeed", TalentCategory.BaseStats, 3, EffectUnit.Percent),
        new TalentDefinition("Breath", TalentCategory.BaseStats, 10, EffectUnit.Percent),
        new TalentDefinition("KnockbackResistance", TalentCategory.BaseStats, .95m, EffectUnit.RemainingMultiplier),
        new TalentDefinition("HeartRecovery", TalentCategory.Recovery, 10, EffectUnit.Percent),
        new TalentDefinition("StarRecovery", TalentCategory.Recovery, 10, EffectUnit.Percent),
        new TalentDefinition("PotionDuration", TalentCategory.Recovery, .93m, EffectUnit.RemainingMultiplier),
        new TalentDefinition("DebuffDuration", TalentCategory.Recovery, .95m, EffectUnit.RemainingMultiplier),
        new TalentDefinition("CritChance", TalentCategory.Combat, 10, EffectUnit.Percent),
        new TalentDefinition("MeleeRange", TalentCategory.Combat, 10, EffectUnit.Percent, true),
        new TalentDefinition("ProjectileSpeed", TalentCategory.Combat, 5, EffectUnit.Percent),
        new TalentDefinition("Invulnerability", TalentCategory.Combat, 1, EffectUnit.Flat),
        new TalentDefinition("Coins", TalentCategory.Economy, 10, EffectUnit.Percent),
        new TalentDefinition("LootQuantity", TalentCategory.Economy, 10, EffectUnit.Percent),
        new TalentDefinition("BagQuantity", TalentCategory.Economy, 10, EffectUnit.Percent),
        new TalentDefinition("DropChance", TalentCategory.Economy, 10, EffectUnit.Percent),
        new TalentDefinition("MiningYield", TalentCategory.Economy, 10, EffectUnit.Percent),
        new TalentDefinition("WoodYield", TalentCategory.Economy, 10, EffectUnit.Percent),
        new TalentDefinition("HerbYield", TalentCategory.Economy, 10, EffectUnit.Percent),
        new TalentDefinition("GemYield", TalentCategory.Economy, 10, EffectUnit.Percent),
        new TalentDefinition("FishingYield", TalentCategory.Economy, 10, EffectUnit.Percent),
        new TalentDefinition("SellPrice", TalentCategory.Economy, 5, EffectUnit.Percent),
        new TalentDefinition("BuyDiscount", TalentCategory.Economy, .95m, EffectUnit.RemainingMultiplier),
        new TalentDefinition("ReforgeDiscount", TalentCategory.Economy, .95m, EffectUnit.RemainingMultiplier),
        new TalentDefinition("ToolReach", TalentCategory.Utility, 1, EffectUnit.Flat, true),
        new TalentDefinition("BuildReach", TalentCategory.Utility, 1, EffectUnit.Flat, true),
        new TalentDefinition("ToolSpeed", TalentCategory.Utility, 20, EffectUnit.Percent, true)
    });
    private static readonly Dictionary<string, TalentDefinition> byId = All.ToDictionary(t => t.Id, StringComparer.Ordinal);
    public static bool TryGet(string id, out TalentDefinition talent) => byId.TryGetValue(id, out talent!);
    public static BigInteger ActiveLevel(ProgressionState state, string id) =>
        state.Talents.TryGetValue(id, out var t) && t.Enabled ? EffectiveLevel(t) : BigInteger.Zero;
    public static BigInteger EffectiveLevel(TalentState t) => t.CurrentIntensity is decimal n ? BigInteger.Min(t.TalentLevel, new BigInteger(n)) : t.TalentLevel;


}

public static class TalentMath
{
    // Additional independent rolls; existing DropChance save ID is retained.
    public static BigInteger ExtraRolls(BigInteger level, double roll)
    {
        var whole = BigInteger.DivRem(BigInteger.Max(0, level), 10, out var remainder);
        return whole + (roll < (double)remainder / 10 ? 1 : 0);
    }
    // Whole tiers and a fractional remainder; no per-tier loops even at huge levels.
    public static BigInteger CritTier(BigInteger talentLevel, double nativeChance, double roll)
    {
        var guaranteed = BigInteger.DivRem(talentLevel, 10, out var remainder);
        double chance = Math.Max(0, double.IsFinite(nativeChance) ? nativeChance : 0) + (double)remainder * 10;
        double tiers = Math.Floor(chance / 100);
        return guaranteed + new BigInteger(tiers) + (roll < (chance % 100) / 100 ? 1 : 0);
    }
    public static int Quantity(int original, BigInteger level, double roll)
    {
        var numerator = (BigInteger)original * (10 + level);
        var whole = BigInteger.DivRem(numerator, 10, out var fraction);
        return (int)BigInteger.Min(int.MaxValue, whole + (roll < (double)fraction / 10 ? 1 : 0));
    }
    // Exact vanilla RollLuck distribution: choose a new denominator uniformly,
    // then roll within it (not a minimum/maximum of two ordinary rolls).
    public static double LuckProbability(int numerator, int denominator, double luck)
    {
        if (numerator <= 0 || denominator <= 0) return 0;
        double normal = Math.Min(1, (double)numerator / denominator);
        if (luck == 0 || !double.IsFinite(luck)) return normal;
        long low = luck > 0 ? denominator / 2 : denominator;
        long high = luck > 0 ? denominator - 1L : denominator * 2L - 1;
        long certainEnd = Math.Min(high, numerator);
        double sum = Math.Max(0, certainEnd - low + 1);
        long from = Math.Max(low, numerator + 1L);
        if (from <= high) sum += numerator * (Harmonic(high) - Harmonic(from - 1));
        double altered = sum / (high - low + 1);
        double weight = Math.Min(1, Math.Abs(luck));
        return Math.Clamp(normal * (1 - weight) + altered * weight, 0, 1);
    }
    private static double Harmonic(long n)
    {
        if (n <= 0) return 0;
        if (n < 64) { double sum = 0; for (int i = 1; i <= n; i++) sum += 1d / i; return sum; }
        double x = n, inv2 = 1 / (x * x);
        return Math.Log(x) + .5772156649015328606 + .5 / x - inv2 / 12 + inv2 * inv2 / 120 - inv2 * inv2 * inv2 / 252;
    }
    public static double DropProbability(double chance, BigInteger level) => 1 - Math.Pow(1 - Math.Clamp(chance, 0, 1), 1 + .1 * Level(level));
    public static double Level(BigInteger n) => (double)BigInteger.Min(n, new BigInteger(double.MaxValue / 1024));
    public static int AddInt(int value, BigInteger extra) => (int)BigInteger.Clamp(value + extra, 0, int.MaxValue);
    public static int ScaleInt(int value, double multiplier) => (int)Math.Clamp(value * multiplier, 0, int.MaxValue);
    public static float Finite(double value) => (float)Math.Clamp(value, 0, float.MaxValue);
    public static double Remaining(double perLevel, BigInteger level) => Math.Pow(perLevel, Level(level));
    // O(1), with a fractional carry. Never loops once per recovered resource point.
    public static int Recover(int current, int maximum, double perSecond, ref double carry)
    {
        if (current >= maximum || perSecond <= 0) { carry = 0; return current; }
        carry += perSecond / 60d;
        int amount = (int)Math.Min(maximum - (long)current, Math.Floor(carry + 1e-9));
        carry = Math.Max(0, carry - amount);
        if (amount == maximum - (long)current) carry = 0;
        return current + amount;
    }
}
