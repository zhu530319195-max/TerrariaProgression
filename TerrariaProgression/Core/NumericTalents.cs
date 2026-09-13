using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TerrariaProgression.Core;

public enum TalentCategory : byte { BaseStats, Recovery, Combat, Economy, Utility, World }
public enum TalentOperation : byte { Upgrade, RefundOne, RefundTalent, Enable, Disable, RefundCategory, RefundEverything, EnableEverything, DisableEverything, DecreaseIntensity, IncreaseIntensity, MaximumIntensity }
public enum TalentResult : byte { Success, NotReady, UnknownTalent, InvalidRequest, NotEnoughPoints, NoChange, StaleRequest, Capacity }
public enum EffectUnit { Flat, Percent, PerSecond, RemainingMultiplier }
public sealed record NumericTalent(string Id, TalentCategory Category, decimal PerLevel, EffectUnit Unit, bool Adjustable = false)
{
    public int DefaultCost => 1;
    public int MaxLevel => 0;
}

// Only implemented and testable entries are exposed to players. P2 will have its
// own explicit FunctionalTalentRegistry and can share the generic save ledger.
public static class NumericTalents
{
    public static readonly IReadOnlyList<NumericTalent> All = Array.AsReadOnly(new[] {
        new NumericTalent("MaxLife", TalentCategory.BaseStats, 25, EffectUnit.Flat),
        new NumericTalent("MaxMana", TalentCategory.BaseStats, 20, EffectUnit.Flat),
        new NumericTalent("Defense", TalentCategory.BaseStats, 4, EffectUnit.Flat),
        new NumericTalent("MoveSpeed", TalentCategory.BaseStats, 5, EffectUnit.Percent),
        new NumericTalent("Acceleration", TalentCategory.BaseStats, 5, EffectUnit.Percent),
        new NumericTalent("LifeRegen", TalentCategory.Recovery, 1, EffectUnit.PerSecond),
        new NumericTalent("NaturalLifeRegen", TalentCategory.Recovery, 10, EffectUnit.Percent),
        new NumericTalent("ManaRegen", TalentCategory.Recovery, 2, EffectUnit.PerSecond),
        new NumericTalent("NaturalManaRegen", TalentCategory.Recovery, 10, EffectUnit.Percent),
        new NumericTalent("Healing", TalentCategory.Recovery, 5, EffectUnit.Percent),
        new NumericTalent("ManaRestoration", TalentCategory.Recovery, 5, EffectUnit.Percent),
        new NumericTalent("Damage", TalentCategory.Combat, 5, EffectUnit.Percent),
        new NumericTalent("AttackSpeed", TalentCategory.Combat, 3, EffectUnit.Percent),
        new NumericTalent("CritDamage", TalentCategory.Combat, 5, EffectUnit.Percent),
        new NumericTalent("ArmorPenetration", TalentCategory.Combat, 3, EffectUnit.Flat),
        new NumericTalent("Knockback", TalentCategory.Combat, 10, EffectUnit.Percent),
        new NumericTalent("AmmoSaving", TalentCategory.Combat, .93m, EffectUnit.RemainingMultiplier),
        new NumericTalent("ManaSaving", TalentCategory.Combat, .95m, EffectUnit.RemainingMultiplier),
        new NumericTalent("Minions", TalentCategory.Combat, 1, EffectUnit.Flat),
        new NumericTalent("Sentries", TalentCategory.Combat, 1, EffectUnit.Flat),
        new NumericTalent("PickupRange", TalentCategory.Economy, 10, EffectUnit.Percent),
        new NumericTalent("JumpSpeed", TalentCategory.BaseStats, 3, EffectUnit.Percent),
        new NumericTalent("Breath", TalentCategory.BaseStats, 10, EffectUnit.Percent),
        new NumericTalent("KnockbackResistance", TalentCategory.BaseStats, .95m, EffectUnit.RemainingMultiplier),
        new NumericTalent("HeartRecovery", TalentCategory.Recovery, 10, EffectUnit.Percent),
        new NumericTalent("StarRecovery", TalentCategory.Recovery, 10, EffectUnit.Percent),
        new NumericTalent("PotionDuration", TalentCategory.Recovery, .93m, EffectUnit.RemainingMultiplier),
        new NumericTalent("DebuffDuration", TalentCategory.Recovery, .95m, EffectUnit.RemainingMultiplier),
        new NumericTalent("CritChance", TalentCategory.Combat, 10, EffectUnit.Percent),
        new NumericTalent("MeleeRange", TalentCategory.Combat, 10, EffectUnit.Percent, true),
        new NumericTalent("ProjectileSpeed", TalentCategory.Combat, 5, EffectUnit.Percent),
        new NumericTalent("Invulnerability", TalentCategory.Combat, 1, EffectUnit.Flat),
        new NumericTalent("Coins", TalentCategory.Economy, 10, EffectUnit.Percent),
        new NumericTalent("LootQuantity", TalentCategory.Economy, 10, EffectUnit.Percent),
        new NumericTalent("BagQuantity", TalentCategory.Economy, 10, EffectUnit.Percent),
        new NumericTalent("DropChance", TalentCategory.Economy, 10, EffectUnit.Percent),
        new NumericTalent("MiningYield", TalentCategory.Economy, 10, EffectUnit.Percent),
        new NumericTalent("WoodYield", TalentCategory.Economy, 10, EffectUnit.Percent),
        new NumericTalent("HerbYield", TalentCategory.Economy, 10, EffectUnit.Percent),
        new NumericTalent("GemYield", TalentCategory.Economy, 10, EffectUnit.Percent),
        new NumericTalent("FishingYield", TalentCategory.Economy, 10, EffectUnit.Percent),
        new NumericTalent("SellPrice", TalentCategory.Economy, 5, EffectUnit.Percent),
        new NumericTalent("BuyDiscount", TalentCategory.Economy, .95m, EffectUnit.RemainingMultiplier),
        new NumericTalent("ReforgeDiscount", TalentCategory.Economy, .95m, EffectUnit.RemainingMultiplier),
        new NumericTalent("ToolReach", TalentCategory.Utility, 1, EffectUnit.Flat, true),
        new NumericTalent("BuildReach", TalentCategory.Utility, 1, EffectUnit.Flat, true)
    });
    private static readonly Dictionary<string, NumericTalent> byId = All.ToDictionary(t => t.Id, StringComparer.Ordinal);
    public static bool TryGet(string id, out NumericTalent talent) => byId.TryGetValue(id, out talent!);
    public static BigInteger ActiveLevel(ProgressionState state, string id) =>
        state.Talents.TryGetValue(id, out var t) && t.Enabled ? EffectiveLevel(t) : BigInteger.Zero;
    public static BigInteger EffectiveLevel(TalentState t) => t.CurrentIntensity is decimal n ? BigInteger.Min(t.TalentLevel, new BigInteger(n)) : t.TalentLevel;

    public static bool ValidateImported(ProgressionState state) => state.Talents.All(pair =>
        byId.TryGetValue(pair.Key, out var def) && (pair.Value.CurrentIntensity == null ||
            (def.Adjustable && pair.Value.CurrentIntensity >= 0 && decimal.Truncate(pair.Value.CurrentIntensity.Value) == pair.Value.CurrentIntensity && new BigInteger(pair.Value.CurrentIntensity.Value) <= pair.Value.TalentLevel)));

    // A bounded request describes intent only. Neither price nor a resulting level
    // is accepted from a client. Clone/validate/commit makes mutations atomic.
    public static TalentResult Apply(ProgressionState original, TalentOperation operation, string id,
        TalentCategory category, int count, out ProgressionState result)
    {
        result = original;
        if (!Enum.IsDefined(operation) || !Enum.IsDefined(category) || count is < 1 or > 100)
            return TalentResult.InvalidRequest;
        if (operation <= TalentOperation.Disable && !TryGet(id, out _)) return TalentResult.UnknownTalent;
        try {
            var next = StateCodec.Decode(StateCodec.Encode(original));
            bool changed = false;
            switch (operation) {
                case TalentOperation.DecreaseIntensity:
                case TalentOperation.IncreaseIntensity:
                case TalentOperation.MaximumIntensity:
                    if (!TryGet(id, out var adjustable) || !adjustable.Adjustable) return TalentResult.InvalidRequest;
                    if (!next.Talents.TryGetValue(id, out var adjustableState)) return TalentResult.NoChange;
                    decimal? intensity = null;
                    if (operation != TalentOperation.MaximumIntensity) {
                        var active = EffectiveLevel(adjustableState);
                        var desired = BigInteger.Clamp(active + (operation == TalentOperation.DecreaseIntensity ? -count : count), 0, adjustableState.TalentLevel);
                        if (desired < adjustableState.TalentLevel) {
                            if (desired > new BigInteger(decimal.MaxValue)) return TalentResult.Capacity;
                            intensity = (decimal)desired;
                        }
                    }
                    changed = adjustableState.CurrentIntensity != intensity;
                    adjustableState.CurrentIntensity = intensity;
                    break;
                case TalentOperation.Upgrade:
                    if (!next.Invest(id, byId[id].DefaultCost, count)) return TalentResult.NotEnoughPoints;
                    changed = true;
                    break;
                case TalentOperation.RefundOne: changed = next.RefundOne(id) > 0; break;
                case TalentOperation.RefundTalent: changed = next.RefundAll(id) > 0; break;
                case TalentOperation.Enable:
                case TalentOperation.Disable:
                    if (next.Talents.TryGetValue(id, out var talent) && talent.Enabled != (operation == TalentOperation.Enable)) {
                        talent.Enabled = operation == TalentOperation.Enable;
                        changed = true;
                    }
                    break;
                default:
                    foreach (var key in next.Talents.Keys.ToArray()) {
                        if (operation == TalentOperation.RefundCategory && (!TryGet(key, out var def) || def.Category != category)) continue;
                        if (operation is TalentOperation.RefundCategory or TalentOperation.RefundEverything) changed |= next.RefundAll(key) > 0;
                        else {
                            bool enabled = operation == TalentOperation.EnableEverything;
                            changed |= next.Talents[key].Enabled != enabled;
                            next.Talents[key].Enabled = enabled;
                        }
                    }
                    break;
            }
            if (!changed) return TalentResult.NoChange;
            foreach (var t in next.Talents.Values)
                if (t.CurrentIntensity is decimal n && new BigInteger(n) > t.TalentLevel) t.CurrentIntensity = null;
            StateCodec.Decode(StateCodec.Encode(next));
            result = next;
            return TalentResult.Success;
        }
        catch (Exception error) when (error is System.IO.IOException or System.IO.InvalidDataException) { return TalentResult.Capacity; }
    }
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
