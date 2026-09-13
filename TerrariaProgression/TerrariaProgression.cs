using System;
using System.IO;
using Terraria.ModLoader;
using TerrariaProgression.Networking;

namespace TerrariaProgression;

public sealed class TerrariaProgression : Mod
{
    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        try { ProgressionNetwork.Receive(reader, whoAmI); }
        catch (Exception error) when (error is IOException or InvalidDataException or ArgumentException or OverflowException or FormatException) {
            Logger.Warn($"Rejected progression packet from slot {whoAmI}: {error.Message}");
        }
    }
}
