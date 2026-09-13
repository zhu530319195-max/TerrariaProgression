using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace TerrariaProgression.Talents;

internal static class BagLootSystem
{
    internal static bool IsContainer(int type) => type > 0 && type < ItemID.Count &&
        (ItemID.Sets.BossBag[type] || ItemID.Sets.IsFishingCrate[type]);
    internal static bool CanBoost(IEntitySource source) => source is EntitySource_ItemOpen open &&
        IsContainer(open.ItemType) && open.Player.active && open.Player.whoAmI == Main.myPlayer &&
        Main.netMode != NetmodeID.Server && ExtendedTalentPlayer.Level(open.Player, "BagQuantity") > 0;
    internal static int Spawn(On_Item.orig_NewItem_Inner orig, IEntitySource source, int x, int y, int width, int height,
        int type, int stack, bool noBroadcast, int prefix, bool noDelay, bool reverse)
    {
        var open = (EntitySource_ItemOpen)source;
        if (type <= 0 || !ContentSamples.ItemsByType.TryGetValue(type, out var sample))
            return orig(source, x, y, width, height, null!, type, stack, noBroadcast, prefix, noDelay, reverse);
        int total = TalentMath.Quantity(stack, ExtendedTalentPlayer.Level(open.Player, "BagQuantity"), Main.rand.NextDouble());
        int maxStack = Math.Max(1, sample.maxStack);
        int primary = Math.Min(total, maxStack), rest = total - primary;
        bool old = EconomySystem.SpawningBonus; EconomySystem.SpawningBonus = true;
        try {
            // Emit overflow BEFORE the returned item: multiplayer uses slot 400 as
            // a temporary outgoing item, so the caller must still find its own item.
            int batches = 0;
            while (rest > 0 && batches++ < Main.maxItems - 1 &&
                (Main.netMode == NetmodeID.MultiplayerClient || ExtraLootSystem.HasRoom(2))) {
                int take = Math.Min(rest, maxStack);
                int slot = orig(source, x, y, width, height, null!, type, take, noBroadcast, prefix, noDelay, reverse);
                if (Main.netMode == NetmodeID.MultiplayerClient) NetMessage.SendData(MessageID.SyncItem, -1, -1, null, slot, 1);
                rest -= take;
            }
            if (rest > 0) ExtraLootSystem.WarnLimit();
            return orig(source, x, y, width, height, null!, type, primary, noBroadcast, prefix, noDelay, reverse);
        }
        finally { EconomySystem.SpawningBonus = old; }
    }
}
