using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader.IO;
using TerrariaProgression.Core;
using TerrariaProgression.Networking;

namespace ProgressionHarness;
public sealed partial class RuntimeChecks
{
    private void RunBulkUpgrades()
    {
        Reset(); A.Award(1000000000 * Experience.Scale);
        Main.netMode = NetmodeID.Server;
        var points = A.State.AvailableTalentPoints;
        int total = 0;
        foreach (int amount in new[] { 10, 100, 300, 1000 }) {
            A.HasActionTick = false;
            ulong revision = A.TalentRevision;
            SendTalentAction(A.SessionId, revision, TalentOperation.Upgrade, "AreaMining", amount);
            total += amount;
            Check(A.State.Talents["AreaMining"].TalentLevel == total && A.State.AvailableTalentPoints == points - total
                && A.TalentRevision == revision + 1, "server decodes complete UInt16 bulk count: " + amount);
            A.HasActionTick = false;
            SendTalentAction(A.SessionId, revision, TalentOperation.Upgrade, "AreaMining", amount);
            Check(A.State.Talents["AreaMining"].TalentLevel == total && A.State.AvailableTalentPoints == points - total,
                "stale bulk request cannot spend twice: " + amount);
        }
        ProgressionNetwork.SendAction(A, TalentOperation.Upgrade, "AreaMining", TalentCategory.Utility, 1000, 9);
        byte[] packet = ((RecordingSocket)Netplay.Clients[0].Socket).LastPacket;
        Check(packet.Length > 2 && packet[^2] == 0xe8 && packet[^1] == 0x03, "production sender writes 1000 without byte truncation");
        var tag = new TagCompound(); A.SaveData(tag); B.LoadData(tag);
        Check(B.State.Talents["AreaMining"].TalentLevel == 1410 && B.State.TotalSpentTalentPoints == 1410,
            "native character save retains bulk levels and exact spend");
        var retained = A.State; A.HasActionTick = false;
        SendTalentAction(A.SessionId, A.TalentRevision, TalentOperation.Upgrade, "AreaMining", 1001);
        Check(ReferenceEquals(retained, A.State), "server rejects oversized count without mutation");
        A.HasActionTick = false;
        SendTalentAction(A.SessionId, A.TalentRevision, TalentOperation.Upgrade, "AutoReplant", 1000);
        Check(ReferenceEquals(retained, A.State), "server cannot bulk-unlock a binary talent");
        using var oldPacket = new MemoryStream(new byte[] { 13, (byte)ProgressionMessage.TalentAction });
        bool rejected = false;
        try { ProgressionNetwork.Receive(new BinaryReader(oldPacket), 0); }
        catch (InvalidDataException) { rejected = true; }
        Check(rejected && ReferenceEquals(retained, A.State), "old protocol is rejected before reading changed count field");
        Reset(); Main.netMode = NetmodeID.Server; A.HasActionTick = false; retained = A.State;
        SendTalentAction(A.SessionId, A.TalentRevision, TalentOperation.Upgrade, "AreaMining", 1000);
        Check(ReferenceEquals(retained, A.State) && A.State.TotalSpentTalentPoints == 0, "server insufficient-points bulk request never partly buys");
        Main.netMode = NetmodeID.SinglePlayer; Reset();
    }
}
