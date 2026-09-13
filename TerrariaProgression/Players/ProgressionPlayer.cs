using System;
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
    internal ulong TalentRevision, LastActionTick, LastSnapshotTick;
    internal bool HasActionTick, HasSnapshotTick;
    internal uint PendingRequest, NextRequest;
    internal TalentResult LastResult = TalentResult.Success;
    internal bool HasResult;
    private DateTime requestStarted;
    internal bool RequestPending => PendingRequest != 0;
    internal bool RequestCoolingDown => Main.netMode == NetmodeID.MultiplayerClient && (DateTime.UtcNow - requestStarted).TotalSeconds < .15;
    internal bool RequestTimedOut => RequestPending && (DateTime.UtcNow - requestStarted).TotalSeconds > 5;
    public override void Initialize()
    {
        State = new();
        SessionReady = false;
        TalentRevision = 0;
        PendingRequest = NextRequest = 0;
        HasActionTick = HasSnapshotTick = HasResult = false;
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
        if (tag.GetInt("DataVersion") is not (1 or ProgressionState.DataVersion))
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
        if (Main.netMode == NetmodeID.Server && SessionReady)
            ProgressionNetwork.SendSnapshot(this, toWho);
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
    internal TalentResult ApplyTalent(TalentOperation operation, string id, TalentCategory category, int count)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !SessionReady || Player.dead) return TalentResult.NotReady;
        var result = NumericTalents.Apply(State, operation, id, category, count, out var updated);
        if (result == TalentResult.Success) { State = updated; TalentRevision++; }
        return result;
    }
    internal void RequestTalent(TalentOperation operation, string id = "", TalentCategory category = TalentCategory.BaseStats, int count = 1)
    {
        if (RequestPending || RequestCoolingDown) return;
        if (Main.netMode == NetmodeID.SinglePlayer) {
            LastResult = ApplyTalent(operation, id, category, count);
            HasResult = true;
        }
        else if (SessionReady) {
            PendingRequest = ++NextRequest;
            if (PendingRequest == 0) PendingRequest = ++NextRequest;
            requestStarted = DateTime.UtcNow;
            HasResult = false;
            ProgressionNetwork.SendAction(this, operation, id, category, count, PendingRequest);
        }
    }
    internal void Acknowledge(uint request, TalentResult result)
    {
        if (request == PendingRequest || (request == 0 && RequestTimedOut)) {
            PendingRequest = 0;
            LastResult = result;
            HasResult = true;
        }
    }
    public override void ProcessTriggers(Terraria.GameInput.TriggersSet triggersSet)
    {
        if (UI.TalentUISystem.ToggleKey?.JustPressed == true) UI.TalentUISystem.Toggle();
    }
    // No death or world save hook modifies progression.
}
