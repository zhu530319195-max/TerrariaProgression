using System;
using System.Collections.Generic;
using System.Numerics;

namespace TerrariaProgression.Core;

public sealed class ProgressionState
{
    public const int DataVersion = 3;
    public BigInteger Level { get; internal set; } = 1;
    public BigInteger CurrentExperience { get; internal set; }
    public BigInteger TotalExperienceEarned { get; internal set; }
    public BigInteger AvailableTalentPoints { get; internal set; }
    public BigInteger TotalTalentPointsEarned { get; internal set; }
    public BigInteger TotalSpentTalentPoints { get; internal set; }
    public Dictionary<string, TalentState> Talents { get; } = new(StringComparer.Ordinal);

    public BigInteger Award(BigInteger units, BigInteger cap, BigInteger? pointsPerLevel = null)
    {
        var reward = pointsPerLevel ?? BigInteger.One;
        if (units < 0 || cap < 0 || reward < 0) throw new ArgumentOutOfRangeException();
        CurrentExperience += units;
        TotalExperienceEarned += units;
        var levels = Experience.LevelsAffordable(Level, CurrentExperience, cap);
        CurrentExperience -= Experience.Cost(Level, levels, cap);
        Level += levels;
        AvailableTalentPoints += levels * reward;
        TotalTalentPointsEarned += levels * reward;
        return levels;
    }

    // The server-facing TalentService supplies the registered ID and price.
    public bool Invest(string id, BigInteger actualCost, BigInteger? levels = null)
    {
        var count = levels ?? BigInteger.One;
        if (string.IsNullOrWhiteSpace(id) || actualCost <= 0 || count <= 0 || actualCost * count > AvailableTalentPoints) return false;
        if (!Talents.TryGetValue(id, out var talent)) Talents[id] = talent = new();
        talent.AddCost(actualCost, count);
        AvailableTalentPoints -= actualCost * count;
        TotalSpentTalentPoints += actualCost * count;
        return true;
    }
    public BigInteger RefundOne(string id)
    {
        if (!Talents.TryGetValue(id, out var talent)) return 0;
        var paid = talent.RemoveLast();
        AvailableTalentPoints += paid;
        TotalSpentTalentPoints -= paid;
        if (talent.TalentLevel == 0) Talents.Remove(id);
        return paid;
    }
    public BigInteger RefundAll(string id)
    {
        if (!Talents.Remove(id, out var talent)) return 0;
        var paid = talent.InvestedPoints;
        AvailableTalentPoints += paid;
        TotalSpentTalentPoints -= paid;
        return paid;
    }
}

public readonly record struct PaidCostRun(BigInteger Cost, BigInteger Count);

public sealed class TalentState
{
    // Adjacent equal prices are compressed. A million levels at the default price
    // occupy one run, while refunds still preserve the exact historical cost.
    public BigInteger TalentLevel { get; private set; }
    public bool Enabled { get; set; } = true;
    public HashSet<string> DisabledEffects { get; } = new(StringComparer.Ordinal);
    private readonly List<PaidCostRun> costs = new();
    public IReadOnlyList<PaidCostRun> CostRuns => costs;
    public BigInteger InvestedPoints { get; private set; }
    public decimal? CurrentIntensity { get; set; }
    internal void AddCost(BigInteger cost, BigInteger count)
    {
        if (cost <= 0 || count <= 0) throw new ArgumentOutOfRangeException();
        if (costs.Count > 0 && costs[^1].Cost == cost) costs[^1] = new(cost, costs[^1].Count + count);
        else costs.Add(new(cost, count));
        TalentLevel += count;
        InvestedPoints += cost * count;
    }
    internal BigInteger RemoveLast()
    {
        var run = costs[^1];
        if (run.Count == 1) costs.RemoveAt(costs.Count - 1);
        else costs[^1] = new(run.Cost, run.Count - 1);
        TalentLevel--;
        InvestedPoints -= run.Cost;
        return run.Cost;
    }
}
