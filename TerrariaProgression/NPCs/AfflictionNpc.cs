using System;
using System.IO;
using System.Linq;
using System.Numerics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace TerrariaProgression.NPCs;

public sealed class AfflictionNpc : GlobalNPC
{
    // Native NPC.UpdateNPC_BuffApplyDOTs in the pinned official binary. These are
    // lifeRegen units (twice DPS), NOT player debuff damage. Ichor has no DoT.
    internal static readonly (string Talent, int Buff, int Regen)[] Effects = {
        ("AttackBurn", BuffID.OnFire, 8), ("AttackPoison", BuffID.Poisoned, 12),
        ("AttackFrostburn", BuffID.Frostburn, 16), ("AttackCursedInferno", BuffID.CursedInferno, 48),
        ("AttackVenom", BuffID.Venom, 60), ("AttackIchor", BuffID.Ichor, 0)
    };
    public override bool InstancePerEntity => true;
    private readonly AfflictionSources sources = new();
    // Numerators / 5 retain +20% exactly with fractional carry, without flooding
    // network updates every time a fractional regen unit is emitted.
    private readonly int[] rates = new int[5];
    private int carry;
    private static ulong world;
    private ulong epoch;
    internal static void ResetWorld() => world++;
    public override void SetDefaults(NPC entity) => Reset();
    private void Reset() { sources.Clear(); Array.Clear(rates); carry = 0; epoch = world; }
    private void CheckWorld() { if (epoch != world) Reset(); }
    private static bool Enemy(NPC npc) => npc.active && npc.life > 0 && !npc.friendly && !npc.townNPC && !npc.immortal && !npc.dontTakeDamage;
    internal static void ApplyHit(NPC npc, Player player, int damage)
    {
        if (!EncounterSystem.Authority || damage <= 0 || !player.active || player.dead || !Enemy(npc)) return;
        var progression = player.GetModPlayer<ProgressionPlayer>();
        if (!progression.SessionReady) return;
        var state = npc.GetGlobalNPC<AfflictionNpc>(); state.CheckWorld();
        bool changed = false;
        for (int i = 0; i < Effects.Length; i++) {
            var (id, buff, _) = Effects[i];
            int duration = TalentMath.AddInt(0, ExtendedTalentPlayer.Level(player, id) * 120);
            if (duration <= 0 || npc.buffImmune[buff]) continue;
            // Prune a missing previous buff before a new source can revive it.
            if (i < 5) state.sources.Highest(i, Main.GameUpdateCount, npc.HasBuff(buff), CurrentLevel);
            npc.AddBuff(buff, duration, quiet: true);
            int index = npc.FindBuffIndex(buff);
            if (index < 0 || npc.buffTime[index] <= 0) continue; // native capacity / ReApply veto
            if (i < 5) state.sources.Apply(i, player.whoAmI, progression.SessionId, Main.GameUpdateCount, Math.Min(duration, npc.buffTime[index]));
            changed = true;
        }
        // AddBuff normally sends before mutation; the authority sends the final
        // combined state once here, including the last newly added buff.
        if (changed && Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.NPCBuffs, -1, -1, null, npc.whoAmI);
    }
    private static BigInteger? CurrentLevel(int index, Guid session)
    {
        var p = Main.player[index];
        if (!p.active) return null;
        var progression = p.GetModPlayer<ProgressionPlayer>();
        return progression.SessionReady && progression.SessionId == session
            ? NumericTalents.ActiveLevel(progression.State, "AfflictionDamage") : null;
    }
    private static bool NativeFlag(NPC npc, int i) => i switch {
        0 => npc.onFire, 1 => npc.poisoned, 2 => npc.onFrostBurn, 3 => npc.onFire2, 4 => npc.venom, _ => false
    };
    public override void UpdateLifeRegen(NPC npc, ref int damage)
    {
        CheckWorld();
        bool enemy = Enemy(npc), changed = false;
        long numerator = 0;
        for (int i = 0; i < 5; i++) {
            if (EncounterSystem.Authority) {
                var level = sources.Highest(i, Main.GameUpdateCount, enemy && npc.HasBuff(Effects[i].Buff), CurrentLevel);
                // Saturate only engine output. Keep saved / active levels unlimited.
                int next = (int)BigInteger.Min(600_000_000, level * Effects[i].Regen);
                changed |= next != rates[i]; rates[i] = next;
            }
            if (enemy && NativeFlag(npc, i) && npc.HasBuff(Effects[i].Buff)) numerator += rates[i];
        }
        if (changed && Main.netMode == NetmodeID.Server) npc.netUpdate = true;
        if (numerator == 0) { carry = 0; return; }
        long total = numerator + carry;
        int extra = (int)Math.Min(120_000_000, total / 5); carry = (int)(total % 5);
        // Do not multiply native totals: oiled, weapon-only statuses and mod DoTs
        // remain unchanged. Bounded popups keep native per-damage loops finite.
        npc.lifeRegen = (int)Math.Max(-1_000_000_000L, (long)Math.Min(0, npc.lifeRegen) - extra);
        damage = Math.Max(damage, Math.Max(1, extra / 120));
    }
    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer)
    {
        CheckWorld(); bool any = rates.Any(n => n > 0); bitWriter.WriteBit(any);
        if (any) foreach (int n in rates) writer.Write(n);
    }
    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader)
    {
        bool any = bitReader.ReadBit();
        // Only authoritative NPC snapshots supply display/prediction rates. No
        // custom client buff/source messages are accepted by the server.
        for (int i = 0; i < 5; i++) {
            int value = any ? reader.ReadInt32() : 0;
            if (Main.netMode == NetmodeID.MultiplayerClient) rates[i] = Math.Clamp(value, 0, 600_000_000);
        }
        if (Main.netMode == NetmodeID.MultiplayerClient) { epoch = world; carry = 0; }
    }
}
