using Terraria;
using Terraria.ModLoader;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace TerrariaProgression.Talents;

public sealed class NumericTalentItem : GlobalItem
{
    public override bool ReforgePrice(Item item, ref int reforgePrice, ref bool canApplyDiscount)
    {
        reforgePrice = TalentMath.ScaleInt(reforgePrice, TalentMath.Remaining(.95, ExtendedTalentPlayer.Level(Main.LocalPlayer, "ReforgeDiscount")));
        return true;
    }
    public override void GrabRange(Item item, Player player, ref int grabRange)
    {
        var progression = player.GetModPlayer<ProgressionPlayer>();
        if (!progression.SessionReady) return;
        grabRange = TalentMath.ScaleInt(grabRange, 1 + .1 * TalentMath.Level(NumericTalents.ActiveLevel(progression.State, "PickupRange")));
    }
}
