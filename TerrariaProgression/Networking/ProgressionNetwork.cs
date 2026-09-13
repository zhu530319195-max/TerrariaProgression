using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace TerrariaProgression.Networking;

internal enum ProgressionMessage : byte { JoinCharacter = 1, Snapshot = 2 }

internal static class ProgressionNetwork
{
    private const byte ProtocolVersion = 1;
    private static ModPacket Packet(ProgressionMessage kind)
    {
        var packet = ModContent.GetInstance<TerrariaProgression>().GetPacket();
        packet.Write(ProtocolVersion);
        packet.Write((byte)kind);
        return packet;
    }
    private static void WriteState(BinaryWriter writer, ProgressionState state)
    {
        byte[] data = StateCodec.Encode(state);
        writer.Write((ushort)data.Length);
        writer.Write(data);
    }
    private static ProgressionState ReadState(BinaryReader reader)
    {
        int length = reader.ReadUInt16();
        if (length > StateCodec.MaxPacketBytes) throw new InvalidDataException("Oversized snapshot.");
        byte[] bytes = reader.ReadBytes(length);
        if (bytes.Length != length) throw new EndOfStreamException();
        var state = StateCodec.Decode(bytes);
        // tML passes its shared message buffer, not a stream bounded to this packet.
        // The length-prefixed StateCodec payload enforces its own exact boundary.
        return state;
    }
    internal static void SendJoin(ProgressionState state)
    {
        var packet = Packet(ProgressionMessage.JoinCharacter);
        WriteState(packet, state);
        packet.Send();
    }
    internal static void SendSnapshot(ProgressionPlayer player)
    {
        var packet = Packet(ProgressionMessage.Snapshot);
        WriteState(packet, player.State);
        packet.Send(player.Player.whoAmI);
    }
    internal static void Receive(BinaryReader reader, int sender)
    {
        if (reader.ReadByte() != ProtocolVersion) throw new InvalidDataException("Unsupported protocol.");
        var kind = (ProgressionMessage)reader.ReadByte();
        if (Main.netMode == NetmodeID.Server) {
            if (kind != ProgressionMessage.JoinCharacter || sender < 0 || sender >= Main.maxPlayers) return;
            var player = Main.player[sender].GetModPlayer<ProgressionPlayer>();
            if (!player.Player.active || player.SessionReady) return;
            var imported = ReadState(reader);
            // No purchased talent exists in P0, so a client cannot introduce hidden talents.
            if (imported.Talents.Count != 0) throw new InvalidDataException("P0 has no registered talents.");
            player.State = imported;
            player.SessionId = Guid.NewGuid();
            player.SessionReady = true;
            SendSnapshot(player);
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient && kind == ProgressionMessage.Snapshot) {
            var state = ReadState(reader);
            var player = Main.LocalPlayer.GetModPlayer<ProgressionPlayer>();
            player.State = state;
            player.SessionReady = true;
        }
    }
}
