using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace TerrariaProgression.Core;

// Runtime policy is deliberately separate from the portable character ledger.
public static class TalentLimits
{
    private static BigInteger global = -1;
    private static Dictionary<string, BigInteger> overrides = new(StringComparer.Ordinal);
    public static int Revision { get; private set; }
    public static bool TryParse(string? text, out BigInteger value) =>
        BigInteger.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value) && value >= -1;
    public static void Configure(string globalLimit, IEnumerable<KeyValuePair<string, string>> entries)
    {
        global = TryParse(globalLimit, out var n) ? n : -1;
        overrides = new(StringComparer.Ordinal);
        foreach (var entry in entries)
            if (TalentCatalog.TryGet(entry.Key, out _) && TryParse(entry.Value, out n)) overrides[entry.Key] = n;
        Revision++;
    }
    public static BigInteger Cap(string id)
    {
        var cap = overrides.GetValueOrDefault(id, global);
        if (TalentCatalog.TryGet(id, out var def) && def.MaxLevel > 0)
            cap = cap < 0 ? def.MaxLevel : BigInteger.Min(cap, def.MaxLevel);
        return cap;
    }
    public static BigInteger Clamp(string id, BigInteger level)
    {
        var cap = Cap(id);
        return BigInteger.Max(0, cap < 0 ? level : BigInteger.Min(level, cap));
    }
    public static int PurchaseCount(ProgressionState state, string id, int requested)
    {
        if (requested < 1 || requested > 1000 || !TalentCatalog.TryGet(id, out var def)) return 0;
        if (def.MaxLevel == 1 && requested != 1) return 0;
        var cap = Cap(id);
        var owned = state.Talents.GetValueOrDefault(id)?.TalentLevel ?? 0;
        return cap < 0 ? requested : (int)BigInteger.Clamp(cap - owned, 0, requested);
    }
}
