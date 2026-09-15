using System;
using System.Numerics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Config;
using TerrariaProgression.Players;

namespace TerrariaProgression.Talents;

public sealed class HerbGrowthSystem : ModSystem
{
    internal const int Radius = 50, MaxExtraUpdatesPerTick = 4096;
    private static ulong budgetTick;
    private static int used;
    public override void Load() => On_WorldGen.GrowAlch += Grow;
    public override void Unload() => On_WorldGen.GrowAlch -= Grow;
    public override void OnWorldUnload() { used = 0; budgetTick = 0; }
    internal static BigInteger Strongest(int x, int y)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return 0;
        var config = ModContent.GetInstance<ProgressionConfig>();
        if (!config.EnableWorldGathering || !config.EnableHerbGrowth || !AgricultureSystem.IsHerb(x,y)) return 0;
        var tile = Main.tile[x,y];
        if (tile.TileFrameX < 0 || tile.TileFrameX > 108 || tile.TileFrameX % 18 != 0) return 0;
        BigInteger highest = 0;
        foreach (var p in Main.ActivePlayers) {
            if (p.dead) continue;
            double dx = p.Center.X / 16d - (x + .5), dy = p.Center.Y / 16d - (y + .5);
            if (dx * dx + dy * dy <= Radius * Radius)
                highest = BigInteger.Max(highest, ExtendedTalentPlayer.Level(p,"HerbGrowth"));
        }
        return highest;
    }
    private static void Grow(On_WorldGen.orig_GrowAlch orig, int x, int y)
    {
        var level = Strongest(x,y);
        orig(x,y); // The native random growth and bloom rules always run once.
        if (level <= 0) return;
        if (budgetTick != Main.GameUpdateCount) { budgetTick = Main.GameUpdateCount; used = 0; }
        // Repeat the exact native herb update, not neighbouring biome/tree updates.
        int extra = (int)BigInteger.Min(level / 5, MaxExtraUpdatesPerTick);
        if (extra < MaxExtraUpdatesPerTick && level % 5 != 0 && WorldGen.genRand.Next(5) < (int)(level % 5)) extra++;
        extra = Math.Min(extra, MaxExtraUpdatesPerTick - used);
        for (int i = 0; i < extra && AgricultureSystem.IsHerb(x,y); i++) { used++; orig(x,y); }
    }
}
