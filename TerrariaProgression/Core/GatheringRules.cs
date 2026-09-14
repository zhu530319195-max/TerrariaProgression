using System;
using System.Collections.Generic;
using System.Numerics;

namespace TerrariaProgression.Core;

public enum GatheringMode : byte { Area, Vein, Tree }
public readonly record struct TilePoint(int X, int Y);
public static class GatheringRules
{
    public static string Talent(GatheringMode mode) => mode switch { GatheringMode.Area => "AreaMining", GatheringMode.Vein => "VeinMining", _ => "TreeFelling" };
    public static int Radius(BigInteger level, int worldExtent) => (int)BigInteger.Clamp(level, 0, Math.Max(0, worldExtent));
    public static long Limit(GatheringMode mode, BigInteger level, int serverLimit, long worldTiles)
    {
        BigInteger max = mode == GatheringMode.Vein ? BigInteger.Max(0, level) * 25 : worldTiles;
        if (serverLimit > 0) max = BigInteger.Min(max, serverLimit);
        return (long)BigInteger.Clamp(max, 0, worldTiles);
    }
    // Lazy concentric squares: budgets prefer the clicked tile's neighbourhood.
    // Every yielded coordinate consumes work, including coordinates outside the world.
    public static IEnumerable<TilePoint> Square(TilePoint center, int radius)
    {
        yield return center;
        for (int d = 1; d <= radius; d++) {
            for (int x = -d; x <= d; x++) yield return new(center.X + x, center.Y - d);
            for (int y = -d + 1; y <= d; y++) yield return new(center.X + d, center.Y + y);
            for (int x = d - 1; x >= -d; x--) yield return new(center.X + x, center.Y + d);
            for (int y = d - 1; y > -d; y--) yield return new(center.X - d, center.Y + y);
        }
    }
    public static IEnumerable<TilePoint> Neighbours(TilePoint p)
    {
        for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) if (x != 0 || y != 0) yield return new(p.X + x, p.Y + y);
    }
}
