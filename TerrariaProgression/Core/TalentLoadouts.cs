using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TerrariaProgression.Core;

public sealed class TalentLoadout
{
    public Guid Id { get; internal set; } = Guid.NewGuid();
    public string Name { get; internal set; } = "方案 1";
    public BigInteger AvailablePoints { get; internal set; }
    public BigInteger SpentPoints { get; internal set; }
    public Dictionary<string, TalentState> Talents { get; } = new(StringComparer.Ordinal);
}

public static class TalentLoadouts
{
    public const int MaxNameLength = 32;
    public static bool ValidName(string name) => name.Length is > 0 and <= MaxNameLength
        && name == name.Trim() && !name.Any(c => char.IsControl(c) || c is '[' or ']')
        && !name.Any(c => char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.Format)
        && ValidSurrogates(name);

    private static bool ValidSurrogates(string name)
    {
        for (int i = 0; i < name.Length; i++) {
            if (char.IsHighSurrogate(name[i])) { if (++i >= name.Length || !char.IsLowSurrogate(name[i])) return false; }
            else if (char.IsLowSurrogate(name[i])) return false;
        }
        return true;
    }

    // Called only on the transaction's private clone. The catalog validates the
    // complete serialized result before the caller replaces the live state.
    internal static TalentResult Apply(ProgressionState state, TalentOperation operation, string value)
    {
        if (operation is TalentOperation.CreateLoadout or TalentOperation.CopyLoadout or TalentOperation.RenameLoadout) {
            value = value.Trim();
            if (!ValidName(value)) return TalentResult.InvalidLoadoutName;
            if (operation == TalentOperation.RenameLoadout) {
                if (state.ActiveLoadout.Name == value) return TalentResult.NoChange;
                state.ActiveLoadout.Name = value;
                return TalentResult.Success;
            }
            var page = new TalentLoadout { Name = value, AvailablePoints = state.TotalTalentPointsEarned };
            if (operation == TalentOperation.CopyLoadout) {
                page.AvailablePoints = state.AvailableTalentPoints;
                page.SpentPoints = state.TotalSpentTalentPoints;
                foreach (var (id, talent) in state.Talents) {
                    var copy = new TalentState { Enabled = talent.Enabled, CurrentIntensity = talent.CurrentIntensity };
                    copy.DisabledEffects.UnionWith(talent.DisabledEffects);
                    foreach (var run in talent.CostRuns) copy.AddCost(run.Cost, run.Count);
                    page.Talents.Add(id, copy);
                }
            }
            state.Pages.Add(page);
            state.ActiveLoadoutId = page.Id;
            return TalentResult.Success;
        }
        if (!Guid.TryParseExact(value, "N", out var target)) return TalentResult.InvalidRequest;
        var found = state.Pages.Find(page => page.Id == target);
        if (found == null) return TalentResult.InvalidRequest;
        if (operation == TalentOperation.ActivateLoadout) {
            if (state.ActiveLoadoutId == target) return TalentResult.NoChange;
            state.ActiveLoadoutId = target;
        }
        else if (operation == TalentOperation.DeleteLoadout) {
            if (state.Pages.Count == 1) return TalentResult.LastLoadout;
            state.Pages.Remove(found);
            if (state.ActiveLoadoutId == target) state.ActiveLoadoutId = state.Pages[0].Id;
        }
        else return TalentResult.InvalidRequest;
        return TalentResult.Success;
    }
}
