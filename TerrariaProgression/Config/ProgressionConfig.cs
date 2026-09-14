using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Newtonsoft.Json;
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

    // Text entry avoids the int/float slider ceiling and rounding of large rewards.
    [DefaultValue("1")]
    public string TalentPointsPerLevel = "1";
    [JsonIgnore] public BigInteger PointsPerLevel => BigInteger.TryParse(TalentPointsPerLevel, NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n >= 0 ? n : BigInteger.One;

    [DefaultValue(1000), Range(1, 100000)]
    public int MaxExtraLootRollsPerEvent = 1000;

    [DefaultValue(true)] public bool BoostMaterials = true;
    [DefaultValue(true)] public bool BoostConsumables = true;
    [DefaultValue(true)] public bool BoostPotions = true;
    [DefaultValue(true)] public bool BoostAmmo = true;
    [DefaultValue(true)] public bool BoostWeapons = true;
    [DefaultValue(true)] public bool BoostArmor = true;
    [DefaultValue(true)] public bool BoostAccessories = true;
    [DefaultValue(true)] public bool BoostBossLoot = true;
    [DefaultValue(true)] public bool BoostTreasureBags = true;
    [DefaultValue(true)] public bool BoostMounts = true;
    [DefaultValue(true)] public bool BoostPets = true;
    [DefaultValue(true)] public bool BoostModItems = true;

    [DefaultValue(true)] public bool EnableWorldGathering = true;
    [DefaultValue(true)] public bool EnableAreaMining = true;
    [DefaultValue(true)] public bool EnableVeinMining = true;
    [DefaultValue(true)] public bool EnableTreeFelling = true;
    [DefaultValue(false)] public bool EnableModOreMining;
    [DefaultValue(1000), Range(0, int.MaxValue)] public int MaxBlocksPerAction = 1000;
    [DefaultValue(32), Range(1, 256)] public int GatheringWorkPerTick = 32;
    public List<ProtectedTileArea> ProtectedTileAreas = new();

    public override void OnChanged()
    {
        if (!BigInteger.TryParse(TalentPointsPerLevel, NumberStyles.None, CultureInfo.InvariantCulture, out var points) || points < 0)
            TalentPointsPerLevel = "1";
        else TalentPointsPerLevel = points.ToString(CultureInfo.InvariantCulture);
        HpToXpMultiplier = float.IsFinite(HpToXpMultiplier) ? Math.Clamp(HpToXpMultiplier, 0.01f, 10f) : 1f;
        StatueExperienceMultiplier = float.IsFinite(StatueExperienceMultiplier) ? Math.Clamp(StatueExperienceMultiplier, 0f, 10f) : 0f;
        ExperienceRequirementCap = Math.Max(0, ExperienceRequirementCap);
        MaxBlocksPerAction = Math.Max(0, MaxBlocksPerAction);
        GatheringWorkPerTick = Math.Clamp(GatheringWorkPerTick, 1, 256);
        ProtectedTileAreas ??= new();
        MaxExtraLootRollsPerEvent = Math.Clamp(MaxExtraLootRollsPerEvent, 1, 100000);
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


public sealed class ProtectedTileArea
{
    [DefaultValue(0), Range(0, int.MaxValue)] public int X;
    [DefaultValue(0), Range(0, int.MaxValue)] public int Y;
    [DefaultValue(1), Range(1, int.MaxValue)] public int Width = 1;
    [DefaultValue(1), Range(1, int.MaxValue)] public int Height = 1;
    public bool Contains(int x, int y) => x >= X && y >= Y && (long)x < (long)X + Width && (long)y < (long)Y + Height;
}
