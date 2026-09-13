using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace TerrariaProgression.Networking;

internal enum ProgressionMessage : byte { JoinCharacter = 1, Snapshot = 2, TalentAction = 3, RequestSnapshot = 4 }

internal static class ProgressionNetwork
{
    internal const byte ProtocolVersion = 9;
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
        return StateCodec.Decode(bytes);
    }
    internal static void SendJoin(ProgressionState state)
    {
        var packet = Packet(ProgressionMessage.JoinCharacter);
        WriteState(packet, state);
        packet.Send();
    }
    internal static void SendSnapshot(ProgressionPlayer player, int? toWho = null, uint acknowledgement = 0, TalentResult result = TalentResult.Success)
    {
        var packet = Packet(ProgressionMessage.Snapshot);
        packet.Write((byte)player.Player.whoAmI);
        packet.Write(player.SessionId.ToByteArray());
        packet.Write(player.TalentRevision);
        packet.Write(acknowledgement);
        packet.Write((byte)result);
        WriteState(packet, player.State);
        packet.Send(toWho ?? player.Player.whoAmI);
    }
    internal static void RequestSnapshot()
    {
        Packet(ProgressionMessage.RequestSnapshot).Send();
    }
    internal static void SendAction(ProgressionPlayer player, TalentOperation operation, string id, TalentCategory category, int count, uint request)
    {
        var packet = Packet(ProgressionMessage.TalentAction);
        packet.Write(player.SessionId.ToByteArray());
        packet.Write(player.TalentRevision);
        packet.Write(request);
        packet.Write((byte)operation);
        packet.Write(id);
        packet.Write((byte)category);
        packet.Write((byte)count);
        packet.Send();
    }
    internal static void Receive(BinaryReader reader, int sender)
    {
        if (reader.ReadByte() != ProtocolVersion) throw new InvalidDataException("Unsupported protocol.");
        var kind = (ProgressionMessage)reader.ReadByte();
        if (Main.netMode == NetmodeID.Server) {
            if (sender < 0 || sender >= Main.maxPlayers) return;
            var player = Main.player[sender].GetModPlayer<ProgressionPlayer>();
            if (!player.Player.active) return;
            if (kind == ProgressionMessage.JoinCharacter) {
                if (player.SessionReady) return;
                var imported = ReadState(reader);
                if (!TalentCatalog.ValidateImported(imported)) throw new InvalidDataException("Unregistered or unsupported talent in character import.");
                player.State = imported;
                player.SessionId = Guid.NewGuid();
                player.TalentRevision = 0;
                player.SessionReady = true;
                SendSnapshot(player, -1);
                // Sync existing characters after the importing client is ready.
                foreach (var other in Main.ActivePlayers) {
                    var progression = other.GetModPlayer<ProgressionPlayer>();
                    if (other.whoAmI != sender && progression.SessionReady) SendSnapshot(progression, sender);
                }
            }
            else if (kind == ProgressionMessage.TalentAction && player.SessionReady) {
                var token = new Guid(reader.ReadBytes(16));
                ulong revision = reader.ReadUInt64();
                uint request = reader.ReadUInt32();
                var operation = (TalentOperation)reader.ReadByte();
                string id = reader.ReadString();
                var category = (TalentCategory)reader.ReadByte();
                int count = reader.ReadByte();
                if (id.Length > 128 || request == 0) return;
                // One accepted request every 6 simulation ticks also bounds clone/encode work.
                if (player.HasActionTick && Main.GameUpdateCount - player.LastActionTick < 6) return;
                player.HasActionTick = true;
                player.LastActionTick = Main.GameUpdateCount;
                var result = token != player.SessionId || revision != player.TalentRevision
                    ? TalentResult.StaleRequest : player.ApplyTalent(operation, id, category, count);
                SendSnapshot(player, result == TalentResult.Success ? -1 : sender, request, result);
            }
            else if (kind == ProgressionMessage.RequestSnapshot && player.SessionReady) {
                if (player.HasSnapshotTick && Main.GameUpdateCount - player.LastSnapshotTick < 60) return;
                player.HasSnapshotTick = true;
                player.LastSnapshotTick = Main.GameUpdateCount;
                SendSnapshot(player);
            }
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient && kind == ProgressionMessage.Snapshot) {
            int slot = reader.ReadByte();
            var session = new Guid(reader.ReadBytes(16));
            ulong revision = reader.ReadUInt64();
            uint acknowledgement = reader.ReadUInt32();
            var result = (TalentResult)reader.ReadByte();
            var state = ReadState(reader);
            if (slot >= Main.maxPlayers || !TalentCatalog.ValidateImported(state)) throw new InvalidDataException("Invalid server talent snapshot.");
            var player = Main.player[slot].GetModPlayer<ProgressionPlayer>();
            if (player.SessionReady && player.SessionId == session && revision < player.TalentRevision) return;
            player.State = state;
            player.SessionId = session;
            player.TalentRevision = revision;
            player.SessionReady = true;
            if (slot == Main.myPlayer) player.Acknowledge(acknowledgement, result);
        }
    }
}
