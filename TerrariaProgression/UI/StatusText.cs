using Terraria.Localization;
using Terraria.ModLoader;
using TerrariaProgression.Config;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace TerrariaProgression.UI;

internal static class StatusText
{
    internal static string For(ProgressionPlayer player)
    {
        var s = player.State;
        string status = Language.GetTextValue("Mods.TerrariaProgression.Messages.Status", s.Level.ToString(),
            Experience.Format(s.CurrentExperience),
            Experience.Format(Experience.Requirement(s.Level, ModContent.GetInstance<ProgressionConfig>().ExperienceRequirementCap)),
            Experience.Format(s.TotalExperienceEarned), s.AvailableTalentPoints.ToString(), s.TotalSpentTalentPoints.ToString());
        return player.SessionReady ? status : Language.GetTextValue("Mods.TerrariaProgression.Messages.Waiting") + "\n" + status;
    }
}
