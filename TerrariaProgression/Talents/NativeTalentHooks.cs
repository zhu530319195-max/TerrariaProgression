using System;
using System.Numerics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace TerrariaProgression.Talents;

// tModLoader's generated On hooks; no reflection or IL rewriting. These narrow
// adapters are used where ModPlayer exposes no final jump/price/pickup hook.
public sealed class NativeTalentHooks : ModSystem
{
    public override void Load()
    {
        On_Player.UpdateJumpHeight += Jump;
        On_Player.GetItemExpectedPrice += Price;
        On_Player.AddBuff_DetermineBuffTimeToAdd += BuffTime;
        On_Player.PickupItem += Pickup;
    }
    public override void Unload()
    {
        On_Player.UpdateJumpHeight -= Jump;
        On_Player.GetItemExpectedPrice -= Price;
        On_Player.AddBuff_DetermineBuffTimeToAdd -= BuffTime;
        On_Player.PickupItem -= Pickup;
    }
    private static void Jump(On_Player.orig_UpdateJumpHeight orig, Player player)
    {
        orig(player);
        // These are static values used during the current player's update, not
        // persistent state. Vanilla resets them before each player's update.
        Player.jumpSpeed = TalentMath.Finite(Player.jumpSpeed * (1 + .03 * TalentMath.Level(ExtendedTalentPlayer.Level(player, "JumpSpeed"))));
    }
    internal static void AdjustPrice(Player player, Item item, ref long sell, ref long buy)
    {
        if (item.shopSpecialCurrency != -1) return;
        // Keep vanilla happiness/discount/buyback calculation; never mutate item.value.
        sell = ScalePrice(sell, 1 + .05 * TalentMath.Level(ExtendedTalentPlayer.Level(player, "SellPrice")));
        buy = ScalePrice(buy, TalentMath.Remaining(.95, ExtendedTalentPlayer.Level(player, "BuyDiscount")));
    }
    private static long ScalePrice(long value, double factor) => value <= 0 ? value :
        (long)Math.Clamp(Math.Floor(value * factor), 1, int.MaxValue - 4096d);
    private static void Price(On_Player.orig_GetItemExpectedPrice orig, Player player, Item item, out long sell, out long buy)
    {
        orig(player, item, out sell, out buy);
        AdjustPrice(player, item, ref sell, ref buy);
    }
    private static int BuffTime(On_Player.orig_AddBuff_DetermineBuffTimeToAdd orig, Player player, int type, int time)
    {
        int result = orig(player, type, time);
        // Only the affected owner shortens it. Relayed duration must not be
        // shortened again by the server or spectators.
        if (player.whoAmI != Main.myPlayer || Main.netMode == NetmodeID.Server || type < 0 || type >= Main.debuff.Length || !Main.debuff[type]) return result;
        double factor = TalentMath.Remaining(.95, ExtendedTalentPlayer.Level(player, "DebuffDuration"));
        if (type == BuffID.PotionSickness) factor *= TalentMath.Remaining(.93, ExtendedTalentPlayer.Level(player, "PotionDuration"));
        return result <= 0 ? result : Math.Max(1, TalentMath.ScaleInt(result, factor));
    }
    private static Item Pickup(On_Player.orig_PickupItem orig, Player player, int playerIndex, int worldIndex, Item item)
    {
        int type = item.type;
        // Small explicit vanilla pickup families, not guesses about modded items.
        int hp = type is ItemID.Heart or ItemID.CandyApple or ItemID.CandyCane ? 20 : 0;
        int mp = type is ItemID.Star or ItemID.SoulCake or ItemID.SugarPlum ? 100 : type == ItemID.ManaCloakStar ? 50 : 0;
        Item result = orig(player, playerIndex, worldIndex, item);
        if (player.whoAmI != Main.myPlayer) return result;
        if (hp > 0) {
            int extra = TalentMath.Quantity(hp, ExtendedTalentPlayer.Level(player, "HeartRecovery"), Main.rand.NextDouble()) - hp;
            extra = Math.Min(extra, player.statLifeMax2 - player.statLife);
            if (extra > 0) player.Heal(extra);
        }
        if (mp > 0) {
            int extra = TalentMath.Quantity(mp, ExtendedTalentPlayer.Level(player, "StarRecovery"), Main.rand.NextDouble()) - mp;
            extra = Math.Min(extra, player.statManaMax2 - player.statMana);
            if (extra > 0) { player.statMana += extra; player.ManaEffect(extra); }
        }
        return result;
    }
}
