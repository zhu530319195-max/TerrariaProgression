using System;
using System.Globalization;
using System.Numerics;

namespace TerrariaProgression.Core;

// Fixed-point micro-XP: exact accumulation, no float overflow or precision loss at high levels.
public static class Experience
{
    public static readonly BigInteger Scale = 1_000_000;
    public static BigInteger FromDecimal(decimal value) => new(decimal.Round(value * (decimal)Scale, 0, MidpointRounding.AwayFromZero));
    public static string Format(BigInteger units)
    {
        var whole = BigInteger.DivRem(units, Scale, out var fraction);
        return whole.ToString(CultureInfo.InvariantCulture) + (fraction.IsZero ? "" : "." + ((int)fraction).ToString("D6").TrimEnd('0'));
    }
    public static bool TryParse(string text, out BigInteger units)
    {
        units = 0;
        if (text.Length is 0 or > 200) return false;
        var parts = text.Split('.');
        if (parts.Length > 2 || parts[0].Length == 0) return false;
        foreach (char c in text) if (c != '.' && (c < '0' || c > '9')) return false;
        if (!BigInteger.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var whole)) return false;
        string fraction = parts.Length == 2 ? parts[1] : "";
        if (fraction.Length > 6) return false;
        units = whole * Scale + int.Parse(fraction.PadRight(6, '0'), CultureInfo.InvariantCulture);
        return true;
    }
    public static BigInteger Requirement(BigInteger level, BigInteger cap)
    {
        var k = level - 1;
        var baseXp = 250 + 50 * k + 10 * k * k;
        return (cap > 0 ? BigInteger.Min(baseXp, cap) : baseXp) * Scale;
    }
    private static BigInteger Prefix(BigInteger n) => 250 * n + 25 * n * (n - 1) + 10 * n * (n - 1) * (2 * n - 1) / 6;
    public static BigInteger Cost(BigInteger level, BigInteger count, BigInteger cap)
    {
        if (count <= 0) return 0;
        if (cap <= 0) return (Prefix(level - 1 + count) - Prefix(level - 1)) * Scale;
        // Find the first capped level inside this range. O(log count), including enormous awards.
        BigInteger lo = 0, hi = count;
        while (lo < hi) {
            var mid = (lo + hi) / 2;
            if (Requirement(level + mid, 0) >= cap * Scale) hi = mid;
            else lo = mid + 1;
        }
        return (Prefix(level - 1 + lo) - Prefix(level - 1) + (count - lo) * cap) * Scale;
    }
    public static BigInteger LevelsAffordable(BigInteger level, BigInteger xp, BigInteger cap)
    {
        BigInteger lo = 0, hi = xp / Requirement(level, cap) + 1;
        while (lo + 1 < hi) {
            var mid = (lo + hi) / 2;
            if (Cost(level, mid, cap) <= xp) lo = mid;
            else hi = mid;
        }
        return lo;
    }
}
