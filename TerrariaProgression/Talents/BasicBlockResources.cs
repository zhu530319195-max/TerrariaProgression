using System.Collections.Generic;
using Terraria.ID;

namespace TerrariaProgression.Talents;

internal static class BasicBlockResources
{
    // Explicit first-batch item whitelist; only consulted for actual tile-break drops
    // after the existing ore/gem/tree/herb adapters have had their single turn.
    internal static readonly HashSet<int> Items = new() {
        ItemID.DirtBlock, ItemID.MudBlock, ItemID.ClayBlock, ItemID.AshBlock,
        ItemID.StoneBlock, ItemID.EbonstoneBlock, ItemID.CrimstoneBlock, ItemID.PearlstoneBlock, ItemID.Granite, ItemID.Marble,
        ItemID.SandBlock, ItemID.EbonsandBlock, ItemID.CrimsandBlock, ItemID.PearlsandBlock,
        ItemID.HardenedSand, ItemID.CorruptHardenedSand, ItemID.CrimsonHardenedSand, ItemID.HallowHardenedSand,
        ItemID.Sandstone, ItemID.CorruptSandstone, ItemID.CrimsonSandstone, ItemID.HallowSandstone,
        ItemID.SnowBlock, ItemID.IceBlock, ItemID.PurpleIceBlock, ItemID.RedIceBlock, ItemID.PinkIceBlock,
        ItemID.SiltBlock, ItemID.SlushBlock, ItemID.DesertFossil
    };
}
