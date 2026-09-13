using System.IO;
using System.Numerics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariaProgression.Config;
using TerrariaProgression.Core;
using TerrariaProgression.Networking;

namespace TerrariaProgression.Players;

public sealed class ProgressionPlayer : ModPlayer
{
    public ProgressionState State { get; internal set; } = new();
    internal bool SessionReady;
    internal System.Guid SessionId;
    public override void Initialize()
    {
        State = new();
        SessionReady = false;
        SessionId = System.Guid.NewGuid();
    }
    public override void SaveData(TagCompound tag)
    {
        tag["DataVersion"] = ProgressionState.DataVersion;
        tag["Progression"] = StateCodec.Encode(State);
    }
    public override void LoadData(TagCompound tag)
    {
        if (!tag.ContainsKey("DataVersion")) { State = new(); return; }
        if (tag.GetInt("DataVersion") != ProgressionState.DataVersion)
            throw new InvalidDataException("Unsupported TerrariaProgression save version; save was not reset.");
        State = StateCodec.Decode(tag.GetByteArray("Progression"));
    }
    public override void OnEnterWorld()
    {
        if (Main.netMode == NetmodeID.SinglePlayer) SessionReady = true;
        else if (Player.whoAmI == Main.myPlayer) {
            SessionReady = false;
            ProgressionNetwork.SendJoin(State);
        }
    }
    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        // Import happens once via OnEnterWorld; never send an uninitialised server state
        // over the player's saved character during the vanilla connection handshake.
        if (Main.netMode == NetmodeID.Server && SessionReady && toWho == Player.whoAmI)
            ProgressionNetwork.SendSnapshot(this);
    }
    public override void PlayerDisconnect()
    {
        SessionReady = false;
        SessionId = System.Guid.NewGuid();
    }
    internal BigInteger Award(BigInteger units)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !SessionReady) return 0;
        var levels = State.Award(units, ModContent.GetInstance<ProgressionConfig>().ExperienceRequirementCap);
        if (Main.netMode == NetmodeID.Server) ProgressionNetwork.SendSnapshot(this);
        return levels;
    }
    // No Kill, UpdateDead, ResetEffects or world save hook modifies progression.
}
