using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Config;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace TerrariaProgression.Talents;

public sealed class AgricultureSystem : ModSystem
{
    // A synchronous, single-tile harvest scope; never claims nearby existing loot.
    private sealed class HarvestScope(Player player, TilePoint tile)
    {
        public readonly Player Player = player;
        public readonly TilePoint Tile = tile;
        public readonly Dictionary<int, Item> Drops = new();
    }
    private static HarvestScope? scope;
    private static ProgressionConfig Config => ModContent.GetInstance<ProgressionConfig>();
    public override void Load()
    {
        On_WorldGen.GetPlayerForTile += ActualActor;
        On_Item.TryCombiningIntoNearbyItems += DeferCombining;
    }
    public override void Unload()
    {
        On_WorldGen.GetPlayerForTile -= ActualActor;
        On_Item.TryCombiningIntoNearbyItems -= DeferCombining;
        scope = null;
    }
    public override void OnWorldUnload() => scope = null;
    private static Player ActualActor(On_WorldGen.orig_GetPlayerForTile orig, int x, int y) =>
        scope is { } s && s.Tile == new TilePoint(x, y) ? s.Player : orig(x, y);
    private static void DeferCombining(On_Item.orig_TryCombiningIntoNearbyItems orig, Item item, int index)
    {
        // Keep newly harvested stacks separate until the one-seed transaction ends.
        // Native item updates can combine the remaining stacks afterwards.
        if (scope?.Drops.TryGetValue(index, out var captured) == true && ReferenceEquals(captured, item)) return;
        orig(item, index);
    }
    internal static void RecordDrop(IEntitySource source, int index)
    {
        if (scope is not { } s || index < 0 || index >= Main.maxItems || source is not EntitySource_TileBreak tile
            || tile.TileCoords.X != s.Tile.X || tile.TileCoords.Y != s.Tile.Y) return;
        s.Drops[index] = Main.item[index];
    }
    internal static bool IsHerb(int x, int y) => WorldGen.InWorld(x, y, 10) && Main.tile[x, y].HasTile
        && Main.tile[x, y].TileType is TileID.ImmatureHerbs or TileID.MatureHerbs or TileID.BloomingHerbs;
    internal static bool ControlsInput(Player p) => p.HeldItem.pick > 0 &&
        ((Config.EnableWorldGathering && Config.EnableAutoReplant && ExtendedTalentPlayer.Level(p, "AutoReplant") > 0) ||
         (p.GetModPlayer<GatheringPlayer>().BatchEnabled && ExtendedTalentPlayer.Level(p, "AreaHarvest") > 0));
    internal static bool CanHarvest(TilePoint pos, bool seedOnly)
    {
        if (!IsHerb(pos.X, pos.Y)) return false;
        var tile = Main.tile[pos.X, pos.Y];
        int style = tile.TileFrameX / 18;
        if (style < 0 || style > 6 || tile.TileFrameX % 18 != 0 || tile.TileType == TileID.ImmatureHerbs) return false;
        return !seedOnly || WorldGen.IsHarvestableHerbWithSeed(tile.TileType, style);
    }
    internal static int Seed(int style) => style switch {
        0 => ItemID.DaybloomSeeds, 1 => ItemID.MoonglowSeeds, 2 => ItemID.BlinkrootSeeds,
        3 => ItemID.DeathweedSeeds, 4 => ItemID.WaterleafSeeds, 5 => ItemID.FireblossomSeeds,
        6 => ItemID.ShiverthornSeeds, _ => 0
    };
    internal static bool Harvest(Player p, TilePoint pos, bool seedOnly)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || scope != null || !CanHarvest(pos, seedOnly)) return false;
        int style = Main.tile[pos.X, pos.Y].TileFrameX / 18;
        var current = new HarvestScope(p, pos);
        scope = current;
        try {
            WorldGen.KillTile(pos.X, pos.Y);
            if (Main.tile[pos.X, pos.Y].HasTile) return false;
            if (Config.EnableWorldGathering && Config.EnableAutoReplant && ExtendedTalentPlayer.Level(p, "AutoReplant") > 0)
                Replant(p, pos, style, current.Drops);
            return true; // A replanted seedling is still one successful harvest.
        }
        finally {
            scope = null;
            if (Main.netMode == NetmodeID.Server) {
                NetMessage.SendTileSquare(-1, pos.X, pos.Y, 1);
                foreach (var pair in current.Drops)
                    if (ReferenceEquals(Main.item[pair.Key], pair.Value)) NetMessage.SendData(MessageID.SyncItem, -1, -1, null, pair.Key);
            }
        }
    }
    internal static bool Replant(Player p, TilePoint pos, int style, IReadOnlyDictionary<int, Item> fresh)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !WorldGen.InWorld(pos.X, pos.Y, 10) || Seed(style) == 0) return false;
        var tile = Main.tile[pos.X, pos.Y];
        if (tile.HasTile || tile.IsActuated || tile.HasActuator || tile.RedWire || tile.BlueWire || tile.GreenWire || tile.YellowWire
            || Config.ProtectedTileAreas.Any(a => a != null && a.Contains(pos.X, pos.Y))) return false;
        int seed = Seed(style), slot = -1;
        Item? supply = null;
        foreach (var pair in fresh) {
            if (pair.Key >= 0 && pair.Key < Main.maxItems && ReferenceEquals(Main.item[pair.Key], pair.Value)
                && pair.Value.active && pair.Value.type == seed && pair.Value.stack > 0) { supply = pair.Value; break; }
        }
        if (supply == null) {
            // Main inventory and hotbar only; no banks, chests, void bag or external storage.
            for (int i = 0; i < 50; i++) if (p.inventory[i].type == seed && p.inventory[i].stack > 0) {
                supply = p.inventory[i]; slot = i; break;
            }
        }
        if (supply == null || !TileLoader.CanPlace(pos.X, pos.Y, TileID.ImmatureHerbs)) return false;
        // PlaceAlch checks the native species-specific substrate, liquid and slope rules.
        // Consume only after verified placement, never on an unsuccessful attempt.
        if (!WorldGen.PlaceAlch(pos.X, pos.Y, style) || !Main.tile[pos.X, pos.Y].HasTile
            || Main.tile[pos.X, pos.Y].TileType != TileID.ImmatureHerbs || Main.tile[pos.X, pos.Y].TileFrameX != style * 18) return false;
        supply.stack--;
        if (supply.stack <= 0) supply.TurnToAir();
        if (slot >= 0 && Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, p.whoAmI, slot, p.inventory[slot].prefix);
        return true;
    }
}
