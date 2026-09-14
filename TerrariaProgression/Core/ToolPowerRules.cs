using System;
using System.Numerics;

namespace TerrariaProgression.Core;

public static class ToolPowerRules
{
    public static int Pick(int original, BigInteger level) => Add(original, level, 10, 100000);
    public static int Axe(int original, BigInteger level) => Add(original, level, 2, 20000);
    private static int Add(int original, BigInteger level, int perLevel, int ceiling) => original <= 0 || level <= 0 ? original
        : (int)BigInteger.Min(Math.Max(original, ceiling), (BigInteger)original + level * perLevel);
}
