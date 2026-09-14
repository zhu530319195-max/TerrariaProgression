using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TerrariaProgression.NPCs;

// Read-only observer of the official v2026.07 StrikeNPC protocol. No custom client
// damage claims, no packet replacement, and no changes to vanilla combat handling.
public sealed class StrikeObserver : ModSystem
{
    private const byte StrikeNpcMessage = 28; // Official MessageBuffer case 28, pinned to v2026.07.
    private sealed record Pending(NpcLifetime Target, NPC HealthOwner, int BeforeLife, int Sender, int Damage, bool InstantKill, ulong Tick);
    private static Pending? pending;
    internal static void Clear() => pending = null;
    public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
    {
        Clear();
        if (Main.netMode != NetmodeID.Server || messageType != StrikeNpcMessage || playerNumber < 0 || playerNumber >= Main.maxPlayers
            || Netplay.Clients[playerNumber].State != 10 || !Main.player[playerNumber].active) return false;
        long position = reader.BaseStream.Position;
        try {
            int slot = reader.ReadInt16();
            int damage = reader.Read7BitEncodedInt();
            if (slot < 0 || slot >= Main.maxNPCs || damage < 0) return false;
            reader.Read7BitEncodedInt(); // SourceDamage
            int damageClass = reader.Read7BitEncodedInt();
            int direction = reader.ReadSByte();
            float knockback = reader.ReadSingle();
            byte flags = reader.ReadByte();
            if (damageClass < 0 || damageClass >= DamageClassLoader.DamageClassCount || !float.IsFinite(knockback) || direction is < -1 or > 1) return false;
            var npc = Main.npc[slot];
            if (!npc.active || npc.life <= 0) return false;
            EncounterSystem.Observe(npc);
            NPC health = npc.realLife >= 0 && npc.realLife < Main.maxNPCs ? Main.npc[npc.realLife] : npc;
            pending = new(EncounterSystem.Get(npc), health, Math.Max(0, health.life), playerNumber, damage, (flags & 2) != 0, Main.GameUpdateCount);
        }
        catch (Exception error) when (error is IOException or FormatException or ArgumentException) { Clear(); }
        finally { reader.BaseStream.Position = position; }
        return false;
    }
    internal static void Confirm(NPC npc, NPC.HitInfo hit)
    {
        var captured = pending;
        if (captured is null || captured.Target != EncounterSystem.Get(npc) || captured.Tick != Main.GameUpdateCount
            || captured.Damage != hit.Damage || captured.InstantKill != hit.InstantKill) return;
        Clear();
        int lost = Math.Max(0, captured.BeforeLife - Math.Max(0, captured.HealthOwner.life));
        int effective = Math.Min(lost, captured.InstantKill ? captured.BeforeLife : captured.Damage);
        EncounterSystem.ReportAttributedDamage(npc, Main.player[captured.Sender], effective);
        AfflictionNpc.ApplyHit(npc, Main.player[captured.Sender], effective);
    }
    public override void PostUpdateEverything() => Clear();
}

