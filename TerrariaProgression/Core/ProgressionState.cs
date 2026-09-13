using System;
using System.Collections.Generic;
using System.Numerics;

namespace TerrariaProgression.Core;

public sealed class ProgressionState
{
    public const int DataVersion = 1;
    public BigInteger Level { get; internal set; } = 1;
    public BigInteger CurrentExperience { get; internal set; }
    public BigInteger TotalExperienceEarned { get; internal set; }
    public BigInteger AvailableTalentPoints { get; internal set; }
    public BigInteger TotalSpentTalentPoints { get; internal set; }
    public Dictionary<string, TalentState> Talents { get; } = new(StringComparer.Ordinal);

    public BigInteger Award(BigInteger units, BigInteger cap, int pointsPerLevel = 1)
    {
        if (units < 0 || cap < 0 || pointsPerLevel < 0) throw new ArgumentOutOfRangeException();
        CurrentExperience += units;
        TotalExperienceEarned += units;
        var levels = Experience.LevelsAffordable(Level, CurrentExperience, cap);
        CurrentExperience -= Experience.Cost(Level, levels, cap);
        Level += levels;
        AvailableTalentPoints += levels * pointsPerLevel;
        return levels;
    }

    // P1/P2 callers must supply a registered talent and a server-approved price.
    // P0 exposes no purchase packet and registers no dummy talents.
    public bool Invest(string id, BigInteger actualCost)
    {
        if (string.IsNullOrWhiteSpace(id) || actualCost <= 0 || actualCost > AvailableTalentPoints) return false;
        if (!Talents.TryGetValue(id, out var talent)) Talents[id] = talent = new();
        talent.PaidCosts.Add(actualCost);
        AvailableTalentPoints -= actualCost;
        TotalSpentTalentPoints += actualCost;
        return true;
    }
    public BigInteger RefundOne(string id)
    {
        if (!Talents.TryGetValue(id, out var talent) || talent.PaidCosts.Count == 0) return 0;
        int index = talent.PaidCosts.Count - 1;
        var paid = talent.PaidCosts[index];
        talent.PaidCosts.RemoveAt(index);
        AvailableTalentPoints += paid;
        TotalSpentTalentPoints -= paid;
        if (talent.PaidCosts.Count == 0) Talents.Remove(id);
        return paid;
    }
}

public sealed class TalentState
{
    public BigInteger TalentLevel => PaidCosts.Count;
    public bool Enabled { get; set; } = true;
    public List<BigInteger> PaidCosts { get; } = new();
    public BigInteger InvestedPoints { get { BigInteger n = 0; foreach (var c in PaidCosts) n += c; return n; } }
    public decimal? CurrentIntensity { get; set; }
}
