using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TerrariaProgression.Core;

public enum TalentCategory : byte { BaseStats, Recovery, Combat, Economy, Utility, World }
public enum TalentOperation : byte { Upgrade, RefundOne, RefundTalent, Enable, Disable, RefundCategory, RefundEverything, EnableEverything, DisableEverything }
public enum TalentResult : byte { Success, NotReady, UnknownTalent, InvalidRequest, NotEnoughPoints, NoChange, StaleRequest, Capacity }
public enum EffectUnit { Flat, Percent, PerSecond, RemainingMultiplier }
public sealed record NumericTalent(string Id, TalentCategory Category, decimal PerLevel, EffectUnit Unit)
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
        new NumericTalent("PickupRange", TalentCategory.Economy, 10, EffectUnit.Percent)
    });
    private static readonly Dictionary<string, NumericTalent> byId = All.ToDictionary(t => t.Id, StringComparer.Ordinal);
    public static bool TryGet(string id, out NumericTalent talent) => byId.TryGetValue(id, out talent!);
    public static BigInteger ActiveLevel(ProgressionState state, string id) =>
        state.Talents.TryGetValue(id, out var t) && t.Enabled ? t.TalentLevel : BigInteger.Zero;

    public static bool ValidateImported(ProgressionState state) => state.Talents.All(pair =>
        byId.ContainsKey(pair.Key) && pair.Value.CurrentIntensity == null);

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
            StateCodec.Decode(StateCodec.Encode(next));
            result = next;
            return TalentResult.Success;
        }
        catch (Exception error) when (error is System.IO.IOException or System.IO.InvalidDataException) { return TalentResult.Capacity; }
    }
}

public static class TalentMath
{
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
