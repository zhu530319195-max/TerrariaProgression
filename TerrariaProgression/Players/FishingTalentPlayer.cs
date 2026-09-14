using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Core;

namespace TerrariaProgression.Players;

public sealed class FishingTalentPlayer : ModPlayer
{
    internal bool? ConserveBait()
    {
        if (Main.netMode == NetmodeID.Server || Player.whoAmI != Main.myPlayer) return null;
        var level = ExtendedTalentPlayer.Level(Player, "BaitSaving");
        if (level <= 0) return null;
        // Never force consumption. Native tackle-box and other vetoes stay intact.
        return Main.rand.NextDouble() < TalentMath.Remaining(.93, level) ? null : false;
    }
    public override void ModifyFishingAttempt(ref FishingAttempt attempt)
    {
        if (Main.netMode == NetmodeID.Server || Player.whoAmI != Main.myPlayer || attempt.crate || attempt.inHoney) return;
        var level = ExtendedTalentPlayer.Level(Player, "CrateChance");
        if (level <= 0) return;
        // Rescue a fraction of failed native crate gates; keep all other fields and
        // the downstream native enemy/quest/liquid/rarity selection untouched.
        if (Main.rand.NextDouble() >= TalentMath.Remaining(.95, level)) attempt.crate = true;
    }
}
