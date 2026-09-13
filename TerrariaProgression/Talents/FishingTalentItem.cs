using Terraria;
using Terraria.ModLoader;
using TerrariaProgression.Players;

namespace TerrariaProgression.Talents;

public sealed class FishingTalentItem : GlobalItem
{
    // Pinned 2026.07 PlayerLoader.CanConsumeBait mistakenly enumerates HookCaughtFish.
    // The official GlobalItem hook dispatches correctly. Use only this route so a
    // future PlayerLoader fix cannot apply our conservation twice.
    public override bool? CanConsumeBait(Player player, Item bait) => player.GetModPlayer<FishingTalentPlayer>().ConserveBait();
}
