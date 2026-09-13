using System;
using Terraria;
using Terraria.ModLoader;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace TerrariaProgression.Talents;

public sealed class ToolEfficiencySystem : ModSystem
{
    internal static Player? MiningPlayer;
    private static bool wallHit;
    public override void Load()
    {
        On_Player.ItemCheck_UseMiningTools += Mining;
        On_Player.ApplyItemTime += Time;
        On_Player.ItemCheck_UseMiningTools_TryHittingWall += Wall;
        On_Player.PickWall += PickWall;
    }
    public override void Unload()
    {
        On_Player.ItemCheck_UseMiningTools -= Mining;
        On_Player.ApplyItemTime -= Time;
        On_Player.ItemCheck_UseMiningTools_TryHittingWall -= Wall;
        On_Player.PickWall -= PickWall;
        MiningPlayer = null; wallHit = false;
    }
    internal static double Factor(Player player) => 1 + .2 * TalentMath.Level(ExtendedTalentPlayer.Level(player, "ToolSpeed"));
    private static void Mining(On_Player.orig_ItemCheck_UseMiningTools orig, Player player, Item item)
    {
        var old = MiningPlayer; MiningPlayer = player;
        try { orig(player, item); }
        finally { MiningPlayer = old; }
    }
    private static void Time(On_Player.orig_ApplyItemTime orig, Player player, Item item, float multiplier, bool? callUseItem)
    {
        if (MiningPlayer == player) multiplier = (float)(multiplier / Factor(player));
        orig(player, item, multiplier, callUseItem);
    }
    private static void Wall(On_Player.orig_ItemCheck_UseMiningTools_TryHittingWall orig, Player player, Item item, int x, int y)
    {
        bool previous = wallHit; wallHit = false;
        try {
            orig(player, item, x, y);
            if (wallHit && MiningPlayer == player && player.itemTime > 0)
                player.itemTime = Math.Max(1, (int)(player.itemTime / Factor(player)));
        }
        finally { wallHit = previous; }
    }
    private static void PickWall(On_Player.orig_PickWall orig, Player player, int x, int y, int damage)
    {
        if (MiningPlayer == player) wallHit = true;
        orig(player, x, y, damage);
    }
}
