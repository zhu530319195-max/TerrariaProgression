using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TerrariaProgression.Core;

// Shared transactions, while each registry owns its own explicit definitions.
public static class TalentCatalog
{
    public static readonly IReadOnlyList<TalentDefinition> All = Array.AsReadOnly(NumericTalents.All.Concat(FunctionalTalentRegistry.All.Select(t => t.Definition)).ToArray());
    private static readonly Dictionary<string, TalentDefinition> byId = All.ToDictionary(t => t.Id, StringComparer.Ordinal);
    public static bool TryGet(string id, out TalentDefinition talent) => byId.TryGetValue(id, out talent!);
    public static bool ValidateImported(ProgressionState state) => state.Talents.All(pair =>
        byId.TryGetValue(pair.Key, out var def) && (def.MaxLevel == 0 || pair.Value.TalentLevel <= def.MaxLevel) &&
        pair.Value.DisabledEffects.All(child => FunctionalTalentRegistry.HasChild(pair.Key, child)) && (pair.Value.CurrentIntensity == null ||
            (def.Adjustable && pair.Value.CurrentIntensity >= 0 && decimal.Truncate(pair.Value.CurrentIntensity.Value) == pair.Value.CurrentIntensity && new BigInteger(pair.Value.CurrentIntensity.Value) <= pair.Value.TalentLevel)));

    // A bounded request describes intent only. Neither price nor a resulting level
    // is accepted from a client. Clone/validate/commit makes mutations atomic.
    public static TalentResult Apply(ProgressionState original, TalentOperation operation, string id,
        TalentCategory category, int count, out ProgressionState result)
    {
        result = original;
        if (!Enum.IsDefined(operation) || !Enum.IsDefined(category) || count is < 1 or > 100)
            return TalentResult.InvalidRequest;
        if (operation <= TalentOperation.Disable && !TryGet(id, out _)) return TalentResult.UnknownTalent;
        if (operation is TalentOperation.RefundMenuGroup or TalentOperation.RefundMenuCategory &&
            !TalentNavigation.ValidScope(id, operation == TalentOperation.RefundMenuGroup)) return TalentResult.InvalidRequest;
        try {
            var next = StateCodec.Decode(StateCodec.Encode(original));
            bool changed = false;
            switch (operation) {
                case TalentOperation.DecreaseIntensity:
                case TalentOperation.IncreaseIntensity:
                case TalentOperation.MaximumIntensity:
                    if (!TryGet(id, out var adjustable) || !adjustable.Adjustable) return TalentResult.InvalidRequest;
                    if (!next.Talents.TryGetValue(id, out var adjustableState)) return TalentResult.NoChange;
                    decimal? intensity = null;
                    if (operation != TalentOperation.MaximumIntensity) {
                        var active = NumericTalents.EffectiveLevel(adjustableState);
                        var desired = BigInteger.Clamp(active + (operation == TalentOperation.DecreaseIntensity ? -count : count), 0, adjustableState.TalentLevel);
                        if (desired < adjustableState.TalentLevel) {
                            if (desired > new BigInteger(decimal.MaxValue)) return TalentResult.Capacity;
                            intensity = (decimal)desired;
                        }
                    }
                    changed = adjustableState.CurrentIntensity != intensity;
                    adjustableState.CurrentIntensity = intensity;
                    break;
                case TalentOperation.Upgrade:
                    var definition = byId[id];
                    if (definition.MaxLevel > 0 && (next.Talents.GetValueOrDefault(id)?.TalentLevel ?? 0) + count > definition.MaxLevel) return TalentResult.NoChange;
                    if (!next.Invest(id, byId[id].DefaultCost, count)) return TalentResult.NotEnoughPoints;
                    changed = true;
                    break;
                case TalentOperation.ToggleChild:
                    if (!FunctionalTalentRegistry.TryGet(id, out var functional) || count > functional.ChildEffects.Count) return TalentResult.InvalidRequest;
                    if (!next.Talents.TryGetValue(id, out var parent)) return TalentResult.NoChange;
                    string child = functional.ChildEffects[count - 1];
                    if (!parent.DisabledEffects.Add(child)) parent.DisabledEffects.Remove(child);
                    changed = true;
                    break;
                case TalentOperation.RefundOne: changed = next.RefundOne(id) > 0; break;
                case TalentOperation.RefundTalent: changed = next.RefundAll(id) > 0; break;
                case TalentOperation.Enable:
                case TalentOperation.Disable:
                    if (next.Talents.TryGetValue(id, out var talent) && talent.Enabled != (operation == TalentOperation.Enable)) {
                        talent.Enabled = operation == TalentOperation.Enable;
                        changed = true;
                    }
                    break;
                default:
                    foreach (var key in next.Talents.Keys.ToArray()) {
                        if (operation is TalentOperation.RefundMenuGroup or TalentOperation.RefundMenuCategory &&
                            !TalentNavigation.InScope(key, id, operation == TalentOperation.RefundMenuGroup)) continue;
                        if (operation == TalentOperation.RefundCategory && (!TryGet(key, out var def) || def.Category != category)) continue;
                        if (operation is TalentOperation.RefundCategory or TalentOperation.RefundEverything or TalentOperation.RefundMenuGroup or TalentOperation.RefundMenuCategory) changed |= next.RefundAll(key) > 0;
                        else {
                            bool enabled = operation == TalentOperation.EnableEverything;
                            changed |= next.Talents[key].Enabled != enabled;
                            next.Talents[key].Enabled = enabled;
                        }
                    }
                    break;
            }
            if (!changed) return TalentResult.NoChange;
            foreach (var t in next.Talents.Values)
                if (t.CurrentIntensity is decimal n && new BigInteger(n) > t.TalentLevel) t.CurrentIntensity = null;
            StateCodec.Decode(StateCodec.Encode(next));
            result = next;
            return TalentResult.Success;
        }
        catch (Exception error) when (error is System.IO.IOException or System.IO.InvalidDataException) { return TalentResult.Capacity; }
    }
}

