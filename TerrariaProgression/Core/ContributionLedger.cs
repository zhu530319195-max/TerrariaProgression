using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TerrariaProgression.Core;

// Session IDs prevent a newly connected character inheriting another player's damage.
public sealed class ContributionLedger
{
    private readonly Dictionary<Guid, BigInteger> damage = new();
    public void Add(Guid player, BigInteger effectiveDamage)
    {
        if (effectiveDamage > 0) damage[player] = damage.GetValueOrDefault(player) + effectiveDamage;
    }
    public void Merge(ContributionLedger other)
    {
        foreach (var (player, amount) in other.damage) Add(player, amount);
    }
    public IReadOnlyDictionary<Guid, BigInteger> Allocate(BigInteger xp)
    {
        var result = new Dictionary<Guid, BigInteger>();
        if (damage.Count == 0 || xp <= 0) return result;
        BigInteger total = damage.Values.Aggregate(BigInteger.Zero, (a, b) => a + b), allocated = 0;
        var remainders = new List<(Guid Player, BigInteger Remainder)>();
        foreach (var (player, amount) in damage) {
            result[player] = BigInteger.DivRem(xp * amount, total, out var remainder);
            allocated += result[player];
            remainders.Add((player, remainder));
        }
        // Largest-remainder apportionment conserves even the final micro-XP.
        foreach (var item in remainders.OrderByDescending(x => x.Remainder).ThenBy(x => x.Player)) {
            if (allocated >= xp) break;
            result[item.Player]++;
            allocated++;
        }
        return result;
    }
}
