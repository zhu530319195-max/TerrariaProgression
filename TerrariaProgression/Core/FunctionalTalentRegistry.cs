using System;
using System.Collections.Generic;
using System.Linq;

namespace TerrariaProgression.Core;

public enum FunctionalImplementation { NativeFlag, NativeSystem, AccessoryBridge, Custom, Composite }
public enum FunctionalStackPolicy { SatisfyOnce, Add, Multiply, Max }
public enum FunctionalAuthority { ConfirmedPlayerState, LocalDisplay, ServerWorld }
public sealed record FunctionalTalent(TalentDefinition Definition, FunctionalImplementation ImplementationKind,
    string Group, FunctionalStackPolicy StackPolicy, FunctionalAuthority NetworkAuthority,
    IReadOnlyList<string> ChildEffects, string CompatibilityGrade = "A", bool DefaultEnabled = true,
    bool RequiresServerPermission = false, bool Experimental = false);

// Explicit registrations only. No item scanning, tooltip inference or accessory emulation.
public static class FunctionalTalentRegistry
{
    public static readonly IReadOnlyList<string> InformationChildren = Array.AsReadOnly(new[] {
        "Time", "Depth", "Compass", "Weather", "Moon", "Fishing", "Radar", "Dps", "RareCreatures", "Ore", "Treasure", "KillCount"
    });
    public static readonly IReadOnlyList<string> ImmunityChildren = Array.AsReadOnly(new[] {
        "Poisoned", "Bleeding", "Slow", "Weak", "BrokenArmor", "Silenced", "Cursed",
        "Confused", "Darkness", "Chilled", "Frozen", "Stoned", "OnFire"
    });
    private static FunctionalTalent Growing(string id, decimal perLevel, EffectUnit unit, FunctionalImplementation kind, FunctionalStackPolicy stack) =>
        new(new TalentDefinition(id, TalentCategory.Combat, perLevel, unit, true), kind,
            "Combat", stack, FunctionalAuthority.ConfirmedPlayerState, Array.Empty<string>(), CompatibilityGrade: kind == FunctionalImplementation.Custom ? "C" : "A");
    private static FunctionalTalent UtilityGrowth(string id, decimal amount, EffectUnit unit, string group, FunctionalStackPolicy stack, FunctionalImplementation kind = FunctionalImplementation.NativeSystem) =>
        new(new TalentDefinition(id, TalentCategory.Utility, amount, unit, true), kind, group, stack,
            FunctionalAuthority.ConfirmedPlayerState, Array.Empty<string>(), CompatibilityGrade: kind == FunctionalImplementation.Custom ? "C" : "A");
    private static FunctionalTalent Unlock(string id, string group, FunctionalImplementation kind = FunctionalImplementation.NativeFlag, TalentCategory category = TalentCategory.Utility) =>
        new(new TalentDefinition(id, category, 0, EffectUnit.Flag, DefaultCost: 2, MaxLevel: 1),
            kind, group, FunctionalStackPolicy.SatisfyOnce, FunctionalAuthority.ConfirmedPlayerState, Array.Empty<string>());
    public static readonly IReadOnlyList<FunctionalTalent> All = Array.AsReadOnly(new[] {
        UtilityGrowth("AreaHarvest", 1, EffectUnit.Flat, "Gathering", FunctionalStackPolicy.Add, FunctionalImplementation.Custom) with { NetworkAuthority = FunctionalAuthority.ServerWorld, RequiresServerPermission = true },
        Unlock("AutoReplant", "Gathering", FunctionalImplementation.Custom) with { NetworkAuthority = FunctionalAuthority.ServerWorld, RequiresServerPermission = true },
        UtilityGrowth("AreaMining", 1, EffectUnit.Flat, "Gathering", FunctionalStackPolicy.Add, FunctionalImplementation.Custom) with { NetworkAuthority = FunctionalAuthority.ServerWorld, RequiresServerPermission = true },
        UtilityGrowth("VeinMining", 25, EffectUnit.Flat, "Gathering", FunctionalStackPolicy.Add, FunctionalImplementation.Custom) with { NetworkAuthority = FunctionalAuthority.ServerWorld, RequiresServerPermission = true },
        Unlock("TreeFelling", "Gathering", FunctionalImplementation.Custom) with { NetworkAuthority = FunctionalAuthority.ServerWorld, RequiresServerPermission = true },
        UtilityGrowth("FlightTime", 1, EffectUnit.Seconds, "Movement", FunctionalStackPolicy.Add),
        UtilityGrowth("FlightSpeed", 5, EffectUnit.Percent, "Movement", FunctionalStackPolicy.Multiply),
        UtilityGrowth("SwimSpeed", 10, EffectUnit.Percent, "Movement", FunctionalStackPolicy.Multiply, FunctionalImplementation.Custom),
        UtilityGrowth("PlacementSpeed", 20, EffectUnit.Percent, "Tools", FunctionalStackPolicy.Multiply),
        UtilityGrowth("WallPlacementSpeed", 20, EffectUnit.Percent, "Tools", FunctionalStackPolicy.Multiply),
        Unlock("NightVision", "Environment") with { NetworkAuthority = FunctionalAuthority.LocalDisplay },
        Unlock("SelfLight", "Environment", FunctionalImplementation.NativeSystem),
        Unlock("DangerSense", "Environment") with { NetworkAuthority = FunctionalAuthority.LocalDisplay },
        Unlock("StatusImmunity", "Immunity", FunctionalImplementation.Composite) with { ChildEffects = ImmunityChildren },
        Growing("StarRetaliation", 1, EffectUnit.Multiplier, FunctionalImplementation.Custom, FunctionalStackPolicy.Multiply),
        Growing("BeeRetaliation", 1, EffectUnit.Multiplier, FunctionalImplementation.Custom, FunctionalStackPolicy.Multiply),
        Unlock("PanicSpeed", "Combat", category: TalentCategory.Combat),
        Growing("AttackBurn", 2, EffectUnit.Seconds, FunctionalImplementation.NativeSystem, FunctionalStackPolicy.Max),
        Growing("AttackPoison", 2, EffectUnit.Seconds, FunctionalImplementation.NativeSystem, FunctionalStackPolicy.Max),
        Growing("AttackFrostburn", 2, EffectUnit.Seconds, FunctionalImplementation.NativeSystem, FunctionalStackPolicy.Max),
        Growing("AttackCursedInferno", 2, EffectUnit.Seconds, FunctionalImplementation.NativeSystem, FunctionalStackPolicy.Max),
        Growing("AttackVenom", 2, EffectUnit.Seconds, FunctionalImplementation.NativeSystem, FunctionalStackPolicy.Max),
        Growing("AttackIchor", 2, EffectUnit.Seconds, FunctionalImplementation.NativeSystem, FunctionalStackPolicy.Max),
        Unlock("Dash", "Movement", FunctionalImplementation.NativeSystem),
        Unlock("WallClimb", "Movement", FunctionalImplementation.NativeSystem),
        Unlock("WallSlide", "Movement", FunctionalImplementation.NativeSystem),
        Unlock("UnlimitedFlight", "Movement", FunctionalImplementation.NativeSystem),
        Unlock("IceTraction", "Movement"),
        Unlock("BuildingRuler", "Tools") with { NetworkAuthority = FunctionalAuthority.LocalDisplay },
        Unlock("AutoPaint", "Tools"),
        Unlock("FishingLine", "Fishing"), Unlock("LavaFishing", "Fishing"),
        Unlock("NoFallDamage", "Movement"),
        new FunctionalTalent(new TalentDefinition("MultiJump", TalentCategory.Utility, 1, EffectUnit.Flat, true),
            FunctionalImplementation.NativeSystem, "Movement", FunctionalStackPolicy.Add, FunctionalAuthority.ConfirmedPlayerState, Array.Empty<string>()),
        Unlock("WaterWalking", "Environment"), Unlock("LavaWalking", "Environment", FunctionalImplementation.NativeSystem),
        Unlock("UnderwaterBreathing", "Environment", FunctionalImplementation.NativeSystem),
        Unlock("LavaImmunity", "Environment"), Unlock("HotTileImmunity", "Environment"),
        Unlock("KnockbackImmunity", "Immunity"),
        Unlock("AllInformation", "Information", FunctionalImplementation.Composite) with {
            ChildEffects = InformationChildren, NetworkAuthority = FunctionalAuthority.LocalDisplay }
    });
    private static readonly Dictionary<string, FunctionalTalent> byId = All.ToDictionary(t => t.Definition.Id, StringComparer.Ordinal);
    public static bool TryGet(string id, out FunctionalTalent talent) => byId.TryGetValue(id, out talent!);
    public static bool HasChild(string id, string child) => TryGet(id, out var t) && t.ChildEffects.Contains(child);
    public static bool ChildEnabled(ProgressionState state, string id, string child) => HasChild(id, child) &&
        state.Talents.TryGetValue(id, out var t) && t.Enabled && !t.DisabledEffects.Contains(child);
    public static string GroupOf(string id) => TryGet(id, out var t) ? t.Group : "Tools";
}

