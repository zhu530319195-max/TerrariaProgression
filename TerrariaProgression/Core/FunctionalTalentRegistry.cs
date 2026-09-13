using System;
using System.Collections.Generic;
using System.Linq;

namespace TerrariaProgression.Core;

public enum FunctionalImplementation { NativeFlag, NativeSystem, AccessoryBridge, Custom, Composite }
public enum FunctionalStackPolicy { SatisfyOnce, Add }
public enum FunctionalAuthority { ConfirmedPlayerState, LocalDisplay }
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
    private static FunctionalTalent Unlock(string id, string group, FunctionalImplementation kind = FunctionalImplementation.NativeFlag) =>
        new(new TalentDefinition(id, TalentCategory.Utility, 0, EffectUnit.Flag, DefaultCost: 2, MaxLevel: 1),
            kind, group, FunctionalStackPolicy.SatisfyOnce, FunctionalAuthority.ConfirmedPlayerState, Array.Empty<string>());
    public static readonly IReadOnlyList<FunctionalTalent> All = Array.AsReadOnly(new[] {
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
