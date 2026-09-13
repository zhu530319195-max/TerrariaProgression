using System;
using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader.Config;

namespace TerrariaProgression.Config;

public sealed class ProgressionConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;
    [DefaultValue(1f), Range(0.01f, 10f), Increment(0.01f)]
    public float HpToXpMultiplier = 1f;
    [DefaultValue(50000), Range(0, int.MaxValue)]
    public int ExperienceRequirementCap = 50000;
    // Design specifies 0, 0.1, 0.5, 1, 2 but no maximum. 0..10 is the P0 input range.
    [DefaultValue(0f), Range(0f, 10f), Increment(0.01f)]
    public float StatueExperienceMultiplier;
    [DefaultValue(false)]
    public bool EnableDebugCommands;
    [DefaultValue(false)]
    public bool LogExperienceSettlements;

    public override void OnChanged()
    {
        HpToXpMultiplier = float.IsFinite(HpToXpMultiplier) ? Math.Clamp(HpToXpMultiplier, 0.01f, 10f) : 1f;
        StatueExperienceMultiplier = float.IsFinite(StatueExperienceMultiplier) ? Math.Clamp(StatueExperienceMultiplier, 0f, 10f) : 0f;
        ExperienceRequirementCap = Math.Max(0, ExperienceRequirementCap);
    }
    public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message)
    {
        // No implicit administrator identity or host IP guesses. Edit rules before hosting
        // or in the dedicated server's configuration file and restart it.
        if (Main.netMode != NetmodeID.Server) return true;
        message = NetworkText.FromKey("Mods.TerrariaProgression.Messages.ServerConfigLocked");
        return false;
    }
}
