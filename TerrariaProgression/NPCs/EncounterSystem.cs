using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Config;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace TerrariaProgression.NPCs;

internal sealed class NpcLifetime(NPC npc, Encounter group)
{
    internal readonly NPC Npc = npc;
    internal Encounter Group = group;
    internal bool Killed;
    internal bool SawLethalHit;
    internal bool SharedRoot;
    internal NpcLifetime? HealthRoot;
    internal int LifeMax = Math.Max(0, npc.lifeMax);
    internal bool Statue = npc.SpawnedFromStatue;
    internal bool Town = npc.townNPC;
}

internal sealed class Encounter
{
    internal Encounter? Parent;
    internal Encounter Root => Parent is null ? this : Parent = Parent.Root;
    internal readonly HashSet<NpcLifetime> Members = new();
    internal readonly ContributionLedger Contributions = new();
    internal bool Settled;
    internal ulong? EmptySince;
}

// Transient combat state only. No player progression is saved to a world.
public sealed class EncounterSystem : ModSystem
{
    private static readonly Dictionary<int, NpcLifetime> lifetimes = new();
    private static readonly HashSet<Encounter> groups = new();
    internal static bool Authority => Main.netMode != NetmodeID.MultiplayerClient;
    internal static NpcLifetime Get(NPC npc)
    {
        if (!lifetimes.TryGetValue(npc.whoAmI, out var node) || !ReferenceEquals(node.Npc, npc)) return Spawn(npc, null);
        return node;
    }
    internal static NpcLifetime Spawn(NPC npc, IEntitySource? source)
    {
        // Capture before replacing a recycled slot; a dead source can occupy that slot.
        NpcLifetime? ancestor = null;
        if (source is EntitySource_Parent { Entity: NPC parent } && !ReferenceEquals(parent, npc)
            && lifetimes.TryGetValue(parent.whoAmI, out var existing) && ReferenceEquals(existing.Npc, parent)) ancestor = existing;
        var group = new Encounter();
        var node = new NpcLifetime(npc, group);
        lifetimes[npc.whoAmI] = node;
        group.Members.Add(node);
        groups.Add(group);
        // Vanilla and compliant content mods supply parent sources for parts, phases,
        // and death splits. Keep one budget for the explicit NPC ancestry.
        if (ancestor is not null) Link(node, ancestor);
        return node;
    }
    private static bool Current(NpcLifetime node) => lifetimes.GetValueOrDefault(node.Npc.whoAmI) == node;
    internal static void Observe(NPC npc)
    {
        var node = Get(npc);
        node.LifeMax = Math.Max(node.LifeMax, npc.lifeMax); // Includes difficulty scaling and transformations.
        node.Statue |= npc.SpawnedFromStatue;
        node.Town |= npc.townNPC;
        if (npc.realLife >= 0 && npc.realLife < Main.maxNPCs && npc.realLife != npc.whoAmI) {
            var root = Main.npc[npc.realLife];
            // Once linked, the same slot number must not bind a leftover body to a
            // completely new NPC that reused its dead root's slot.
            bool newHealthSlot = node.HealthRoot is null || node.HealthRoot.Npc.whoAmI != npc.realLife;
            if (root.active && (newHealthSlot || ReferenceEquals(node.HealthRoot, lifetimes.GetValueOrDefault(npc.realLife)))) {
                var rootNode = Get(root);
                node.HealthRoot = rootNode;
                rootNode.SharedRoot = true;
                Link(node, rootNode);
            }
        }
        // Eater-style worms have independently killable segments and realLife == -1.
        // Verify reciprocal AI links; never group merely by type, position, or boss flag.
        if (npc.aiStyle == NPCAIStyleID.Worm) {
            for (int i = 0; i < 2; i++) {
                float value = npc.ai[i];
                if (value != (int)value || value < 0 || value >= Main.maxNPCs || value == npc.whoAmI) continue;
                var other = Main.npc[(int)value];
                if (other.active && other.aiStyle == npc.aiStyle && other.ai[1 - i] == npc.whoAmI) Link(node, Get(other));
            }
        }
    }
    internal static void Link(NpcLifetime child, NpcLifetime parent)
    {
        var a = child.Group.Root;
        var b = parent.Group.Root;
        if (ReferenceEquals(a, b)) return;
        // A previously paid ancestor never generates another payout via a late child.
        b.Settled |= a.Settled;
        b.Contributions.Merge(a.Contributions);
        foreach (var member in a.Members) b.Members.Add(member);
        a.Parent = b;
        b.EmptySince = null;
        groups.Remove(a);
    }
    public static void LinkEncounter(NPC child, NPC parent)
    {
        if (Authority) Link(Get(child), Get(parent));
    }
    public static void ReportAttributedDamage(NPC npc, Player player, int effectiveDamage)
    {
        if (!Authority || !player.active || effectiveDamage <= 0) return;
        Observe(npc);
        var progression = player.GetModPlayer<ProgressionPlayer>();
        var group = Get(npc).Group.Root;
        if (progression.SessionReady && !group.Settled) group.Contributions.Add(progression.SessionId, effectiveDamage);
    }
    internal static void Killed(NPC npc)
    {
        Observe(npc);
        Get(npc).Killed = true;
    }
    public override void PostUpdateEverything() => UpdateEncounters(Main.GameUpdateCount);
    internal static void UpdateEncounters(ulong tick)
    {
        if (!Authority) return;
        foreach (var group in groups.ToArray()) {
            bool alive = false;
            foreach (var node in group.Members) {
                // Statue loot filtering can skip OnKill. Require a lethal hit witness
                // before using the inactive fallback; despawn alone is not a kill.
                if (node.SawLethalHit && !node.Npc.active && node.Npc.life <= 0) node.Killed = true;
                if (Current(node) && node.Npc.active && !node.Killed) alive = true;
            }
            if (alive) { group.EmptySince = null; continue; }
            group.EmptySince ??= tick;
            // OnKill runs inside StrikeNPC, before OnHitByItem/Projectile. Settle later
            // so fatal hits, same-frame splits and transformations cannot be omitted.
            if (tick - group.EmptySince < 2) continue;
            bool completed = group.Members.All(n => n.Killed) || group.Members.Any(n => n.SharedRoot && n.Killed);
            if (!group.Settled && completed) Settle(group);
            group.Settled = true;
            groups.Remove(group);
        }
    }
    private static void Settle(Encounter group)
    {
        group.Settled = true;
        // Conservative multipart policy: max lifeMax once, never the sum of segment HP.
        // Explicit ancestry also prevents death splits and boss minions minting budgets.
        if (group.Members.Any(n => n.Town)) return;
        var config = ModContent.GetInstance<ProgressionConfig>();
        int maximumLife = group.Members.Max(n => n.LifeMax);
        decimal source = group.Members.Any(n => n.Statue) ? (decimal)config.StatueExperienceMultiplier : 1m;
        var xp = Experience.FromDecimal(maximumLife * (decimal)config.HpToXpMultiplier * source);
        foreach (var (session, share) in group.Contributions.Allocate(xp)) {
            var recipient = Main.player.FirstOrDefault(p => p.active && p.GetModPlayer<ProgressionPlayer>().SessionReady && p.GetModPlayer<ProgressionPlayer>().SessionId == session);
            if (recipient is not null) recipient.GetModPlayer<ProgressionPlayer>().Award(share);
            if (config.LogExperienceSettlements)
                ModContent.GetInstance<TerrariaProgression>().Logger.Info($"XP settlement: lifeMax={maximumLife}, members={group.Members.Count}, share={Experience.Format(share)}, recipient={(recipient is null ? "disconnected (not reassigned)" : recipient.whoAmI.ToString())}");
        }
    }
    public override void ClearWorld()
    {
        lifetimes.Clear();
        groups.Clear();
        StrikeObserver.Clear();
    }
}
