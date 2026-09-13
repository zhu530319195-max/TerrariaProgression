using System;
using System.Numerics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Config;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace TerrariaProgression.Talents;

public sealed class EconomySystem : ModSystem
{
    // Main-thread call scopes, restored even when a content mod throws. No nearest
    // player guesses and no attribution retained after the originating operation.
    internal static Player? Actor;
    internal static DropAttemptInfo? DropContext;
    internal static int BreakingTile = -1;
    internal static bool SpawningBonus;
    private static bool warnedCapacity;
    public override void Load()
    {
        On_Item.NewItem_Inner += SpawnItem;
        On_Player.ItemCheck += ItemCheck;
        On_Player.PickTile += Pick;
        On_MessageBuffer.GetData += Message;
        On_WorldGen.KillTile += KillTile;
        On_ItemDropResolver.TryDropping += Drop;
        On_CommonDrop.TryDroppingItem += Common;
        On_CommonCode.DropItemLocalPerClientAndSetNPCMoneyTo0 += Instanced;
        On_CommonCode.DropItemForEachInteractingPlayerOnThePlayer += PerPlayer;
    }
    public override void Unload()
    {
        On_Item.NewItem_Inner -= SpawnItem;
        On_Player.ItemCheck -= ItemCheck;
        On_Player.PickTile -= Pick;
        On_MessageBuffer.GetData -= Message;
        On_WorldGen.KillTile -= KillTile;
        On_ItemDropResolver.TryDropping -= Drop;
        On_CommonDrop.TryDroppingItem -= Common;
        On_CommonCode.DropItemLocalPerClientAndSetNPCMoneyTo0 -= Instanced;
        On_CommonCode.DropItemForEachInteractingPlayerOnThePlayer -= PerPlayer;
        Clear();
        ResourceAdapters.Clear();
    }
    public override void OnWorldUnload() => Clear();
    private static void Clear() { Actor = null; DropContext = null; BreakingTile = -1; SpawningBonus = warnedCapacity = false; }
    private static int SpawnItem(On_Item.orig_NewItem_Inner orig, IEntitySource source, int x, int y, int width, int height, Item clone, int type, int stack, bool noBroadcast, int prefix, bool noDelay, bool reverse)
    {
        bool old = SpawningBonus;
        // Private/instanced item recipients are assigned AFTER OnSpawn. Until an
        // explicit per-recipient adapter is present, do not turn them into public
        // bonus items or use another player's talent for everyone's private bag.
        if (Main.netMode == NetmodeID.Server && noBroadcast) SpawningBonus = true;
        try { return orig(source, x, y, width, height, clone, type, stack, noBroadcast, prefix, noDelay, reverse); }
        finally { SpawningBonus = old; }
    }
    private static void ItemCheck(On_Player.orig_ItemCheck orig, Player player)
    {
        var old = Actor; Actor = player;
        try { orig(player); } finally { Actor = old; }
    }
    private static void Pick(On_Player.orig_PickTile orig, Player player, int x, int y, int power)
    {
        var old = Actor; Actor = player;
        try { orig(player, x, y, power); } finally { Actor = old; }
    }
    private static void Message(On_MessageBuffer.orig_GetData orig, MessageBuffer buffer, int start, int length, out int messageType)
    {
        var old = Actor;
        // Terraria validates and performs the ordinary tile action. We only attach
        // the actual sender to drops produced inside that action, never create a
        // separate client request to award resources.
        if (Main.netMode == NetmodeID.Server && start >= 0 && start < buffer.readBuffer.Length && length > 0 &&
            buffer.readBuffer[start] == MessageID.TileManipulation && buffer.whoAmI >= 0 && buffer.whoAmI < Main.maxPlayers)
            Actor = Main.player[buffer.whoAmI];
        try { orig(buffer, start, length, out messageType); } finally { Actor = old; }
    }
    private static void KillTile(On_WorldGen.orig_KillTile orig, int i, int j, bool fail, bool effectOnly, bool noItem)
    {
        int old = BreakingTile;
        BreakingTile = !fail && !effectOnly && !noItem && WorldGen.InWorld(i, j) && Main.tile[i, j].HasTile ? Main.tile[i, j].TileType : -1;
        try { orig(i, j, fail, effectOnly, noItem); } finally { BreakingTile = old; }
    }
    private static void Drop(On_ItemDropResolver.orig_TryDropping orig, ItemDropResolver resolver, DropAttemptInfo info)
    {
        var old = DropContext; DropContext = info;
        try { orig(resolver, info); } finally { DropContext = old; }
    }
    private static ItemDropAttemptResult Common(On_CommonDrop.orig_TryDroppingItem orig, CommonDrop rule, DropAttemptInfo info)
    {
        // Do not guess the semantics of subclass overrides, pity systems, options
        // pools or third-party rules. Their original code remains in control.
        if (Main.netMode == NetmodeID.MultiplayerClient || info.npc == null || rule.GetType() != typeof(CommonDrop) ||
            info.player == null || !Allowed(ContentSamples.ItemsByType[rule.itemId], info.npc)) return orig(rule, info);
        var level = ExtendedTalentPlayer.Level(info.player, "DropChance");
        if (level <= 0 || rule.chanceDenominator <= 0) return orig(rule, info);
        double baseline = TalentMath.LuckProbability(rule.chanceNumerator, rule.chanceDenominator, info.player.luck);
        if (info.rng.NextDouble() >= TalentMath.DropProbability(baseline, level))
            return new ItemDropAttemptResult { State = ItemDropAttemptResultState.FailedRandomRoll };
        CommonCode.DropItem(info, rule.itemId, info.rng.Next(rule.amountDroppedMinimum, rule.amountDroppedMaximum + 1));
        return new ItemDropAttemptResult { State = ItemDropAttemptResultState.Success };
    }
    private static void Instanced(On_CommonCode.orig_DropItemLocalPerClientAndSetNPCMoneyTo0 orig, NPC npc, int id, int stack, bool required)
    {
        bool old = SpawningBonus;
        if (Main.netMode == NetmodeID.Server) SpawningBonus = true;
        try { orig(npc, id, stack, required); } finally { SpawningBonus = old; }
    }
    private static void PerPlayer(On_CommonCode.orig_DropItemForEachInteractingPlayerOnThePlayer orig, NPC npc, int id, Terraria.Utilities.UnifiedRandom rng, int numerator, int denominator, int stack, bool required)
    {
        bool old = SpawningBonus;
        if (Main.netMode == NetmodeID.Server) SpawningBonus = true;
        try { orig(npc, id, rng, numerator, denominator, stack, required); } finally { SpawningBonus = old; }
    }
    internal static bool Allowed(Item item, NPC? npc)
    {
        var c = ModContent.GetInstance<ProgressionConfig>();
        if (npc?.boss == true && !c.BoostBossLoot) return false;
        if (item.ModItem != null && !c.BoostModItems) return false;
        if (ItemID.Sets.BossBag[item.type]) return c.BoostTreasureBags;
        if (item.mountType > -1) return c.BoostMounts;
        if (item.buffType > 0 && (Main.vanityPet[item.buffType] || Main.lightPet[item.buffType])) return c.BoostPets;
        if (item.accessory) return c.BoostAccessories;
        if (item.headSlot >= 0 || item.bodySlot >= 0 || item.legSlot >= 0) return c.BoostArmor;
        if (item.ammo > 0) return c.BoostAmmo;
        if (item.damage > 0) return c.BoostWeapons;
        if (item.potion || item.healLife > 0 || item.healMana > 0 || item.buffType > 0) return c.BoostPotions;
        return item.consumable && item.createTile == -1 ? c.BoostConsumables : c.BoostMaterials;
    }
    internal static string? Resource(int tile, Item item)
    {
        if (tile < 0) return null;
        // Vanilla item families; unknown modded sources can register explicitly.
        if (ResourceAdapters.TryGetValue(tile, out var adapter)) return adapter(item);
        if (item.type is ItemID.Amethyst or ItemID.Topaz or ItemID.Sapphire or ItemID.Emerald or ItemID.Ruby or ItemID.Diamond or ItemID.Amber) return "GemYield";
        if (TileID.Sets.Ore[tile]) return "MiningYield";
        if (TileID.Sets.IsATreeTrunk[tile] || TileID.Sets.CountsAsGemTree[tile])
            return item.createTile > -1 && !ItemID.Sets.IsAPickup[item.type] && item.type != ItemID.Acorn ? "WoodYield" : null;
        if (tile is TileID.ImmatureHerbs or TileID.MatureHerbs or TileID.BloomingHerbs) return "HerbYield";
        return null;
    }
    // Explicit extension point; no mod-name matching or reflection.
    public static System.Collections.Generic.Dictionary<int, Func<Item, string?>> ResourceAdapters { get; } = new();
    internal static void BoostWorld(Item item, IEntitySource source, BigInteger level)
    {
        if (level <= 0 || item.stack <= 0) return;
        int desired = TalentMath.Quantity(item.stack, level, Main.rand.NextDouble());
        int first = Math.Min(desired, Math.Max(1, item.maxStack));
        int remaining = desired - first;
        item.stack = first;
        bool previous = SpawningBonus; SpawningBonus = true;
        try {
            // Bounded by the native world item pool; don't overwrite existing loot.
            while (remaining > 0) {
                bool room = false;
                for (int i = 0; i < Main.maxItems; i++) if (!Main.item[i].active) { room = true; break; }
                if (!room) { WarnCapacity(); break; }
                int stack = Math.Min(remaining, Math.Max(1, item.maxStack));
                var bonus = item.Clone(); bonus.stack = stack;
                Item.NewItem(source, item.Hitbox, bonus);
                remaining -= stack;
            }
        }
        finally { SpawningBonus = previous; }
    }
    internal static void BoostInventoryCatch(Player player, Item fish, BigInteger level)
    {
        if (level <= 0) return;
        int desired = TalentMath.Quantity(fish.stack, level, Main.rand.NextDouble());
        fish.stack = Math.Min(desired, Math.Max(1, fish.maxStack));
        int remainder = desired - fish.stack;
        // Caught item has not entered inventory yet. Excess becomes ordinary loot;
        // do not feed it through a fishing or pickup multiplier again.
        if (remainder > 0) {
            bool old = SpawningBonus; SpawningBonus = true;
            try {
                for (int n = 0; remainder > 0 && n < Main.maxItems; n++) {
                    if (Main.item[n].active) continue;
                    int stack = Math.Min(remainder, Math.Max(1, fish.maxStack));
                    Item.NewItem(player.GetSource_Misc("TerrariaProgression/FishingOverflow"), player.Hitbox, fish.type, stack);
                    remainder -= stack;
                }
                if (remainder > 0) WarnCapacity();
            } finally { SpawningBonus = old; }
        }
    }
    private static void WarnCapacity()
    {
        if (warnedCapacity) return;
        warnedCapacity = true;
        ModContent.GetInstance<TerrariaProgression>().Logger.Warn("Bonus item output reached Terraria's world item capacity; excess could not be spawned. Talent levels were not changed.");
    }
}

public sealed class EconomyItem : GlobalItem
{
    public override void OnSpawn(Item item, IEntitySource source)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || EconomySystem.SpawningBonus || ItemID.Sets.IsAPickup[item.type]) return;
        NPC? npc = source is EntitySource_Loot loot ? loot.Entity as NPC : null;
        Player? player = null;
        string? id = null;
        if (npc != null) {
            player = EconomySystem.DropContext?.player;
            if (player == null && npc.lastInteraction >= 0 && npc.lastInteraction < Main.maxPlayers) player = Main.player[npc.lastInteraction];
            if (item.IsACoin) id = "Coins";
            else if (EconomySystem.Allowed(item, npc)) id = "LootQuantity";
        }
        else if (source is EntitySource_TileBreak && EconomySystem.Actor is Player actor) {
            player = actor; id = EconomySystem.Resource(EconomySystem.BreakingTile, item);
        }
        if (player?.active != true || id == null) return;
        EconomySystem.BoostWorld(item, source, ExtendedTalentPlayer.Level(player, id));
    }
}
