using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TerrariaProgression.Core;

// Transient leases, never saved. Each application owns only its requested lifetime,
// not the longer duration that an unrelated weapon may leave on the native buff.
public sealed class AfflictionSources
{
    private sealed record Lease(Guid Session, ulong Expires);
    private readonly Dictionary<(int Kind, int Player), Lease> leases = new();
    public void Apply(int kind, int player, Guid session, ulong tick, int duration)
    {
        if (duration <= 0 || kind is < 0 or >= 5 || player is < 0 or >= 255) return;
        ulong expires = tick > ulong.MaxValue - (uint)duration ? ulong.MaxValue : tick + (uint)duration;
        var key = (kind, player);
        if (leases.TryGetValue(key, out var old) && old.Session == session) expires = Math.Max(expires, old.Expires);
        leases[key] = new(session, expires);
    }
    public BigInteger Highest(int kind, ulong tick, bool present, Func<int, Guid, BigInteger?> currentLevel)
    {
        BigInteger best = 0;
        foreach (var (key, lease) in leases.Where(p => p.Key.Kind == kind).ToArray()) {
            var level = present && tick < lease.Expires ? currentLevel(key.Player, lease.Session) : null;
            if (level is null) leases.Remove(key);
            else best = BigInteger.Max(best, level.Value);
        }
        return best;
    }
    public void Clear() => leases.Clear();
}
