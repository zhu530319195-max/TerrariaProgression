using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Core;

namespace TerrariaProgression.Players;

public sealed class FishingTalentPlayer : ModPlayer
{
    public override void UpdateEquips()
    {
        var level = ExtendedTalentPlayer.Level(Player, "FishingPower");
        if (level <= 0) return;
        // GetFishingLevel receives an environment MULTIPLIER in the pinned native
        // build. Add to equipment power here instead; native weather/time still apply.
        Player.fishingSkill = (int)System.Numerics.BigInteger.Min(1_000_000,
            (System.Numerics.BigInteger)Player.fishingSkill + 5 * level);
    }
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
