using System;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.ID;
using TerrariaProgression.Networking;
using TerrariaProgression.Core;
using TerrariaProgression.Talents;
using TerrariaProgression.UI;

namespace TerrariaProgression.Players;

public sealed class GatheringPlayer : ModPlayer
{
    public GatheringMode MiningMode = GatheringMode.Area;
    internal uint Sequence;
    internal bool ProtectionEnabled; // Default off on every world entry.
    internal uint ProtectionSequence;
    internal bool BatchEnabled; // Session-only: deliberately not saved.
    internal void ToggleBatch()
    {
        var state = Player.GetModPlayer<ProgressionPlayer>();
        if (!state.SessionReady) return;
        bool enabled = !BatchEnabled;
        uint sequence = ++Sequence;
        if (Main.netMode == NetmodeID.MultiplayerClient) {
            BatchEnabled = enabled;
            ProgressionNetwork.SendGatheringToggle(state, enabled, sequence);
        }
        else GatheringSystem.SetBatch(Player, state.SessionId, sequence, enabled);
        Main.NewText(Terraria.Localization.Language.GetTextValue("Mods.TerrariaProgression.Messages." + (BatchEnabled ? "GatheringOn" : "GatheringOff")));
    }
    internal void ToggleProtection()
    {
        var state = Player.GetModPlayer<ProgressionPlayer>();
        if (!state.SessionReady) return;
        bool enabled = !ProtectionEnabled;
        ProtectionSequence = ++Sequence;
        if (Main.netMode == NetmodeID.MultiplayerClient) {
            ProtectionEnabled = enabled;
            ProgressionNetwork.SendProtectionToggle(state, enabled, ProtectionSequence);
        }
        else GatheringSystem.SetProtection(Player, state.SessionId, ProtectionSequence, enabled);
        Main.NewText(Terraria.Localization.Language.GetTextValue("Mods.TerrariaProgression.Messages." + (ProtectionEnabled ? "MiningProtectionOn" : "MiningProtectionOff")));
    }
    internal static bool InputAllowed => !Main.gameMenu && !Main.drawingPlayerChat && !Main.editSign && !Main.editChest && !TalentUISystem.Visible && !Main.LocalPlayer.mouseInterface;
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (!InputAllowed || Player.dead) return;
        if (GatheringSystem.ActionKey?.JustPressed == true) ToggleBatch();
        if (GatheringSystem.ProtectionKey?.JustPressed == true) ToggleProtection();
        if (GatheringSystem.ModeKey?.JustPressed == true) {
            MiningMode = MiningMode == GatheringMode.Area ? GatheringMode.Vein : GatheringMode.Area;
            Main.NewText(Terraria.Localization.Language.GetTextValue("Mods.TerrariaProgression.Messages.GatheringMode", ModeName(MiningMode)));
        }
    }
    internal static string ModeName(GatheringMode mode) => Terraria.Localization.Language.GetTextValue("Mods.TerrariaProgression.TalentNames." + GatheringRules.Talent(mode));
    public override void SaveData(TagCompound tag) => tag["MiningMode"] = (int)MiningMode;
    public override void LoadData(TagCompound tag) => MiningMode = tag.GetInt("MiningMode") == 1 ? GatheringMode.Vein : GatheringMode.Area;
    public override void OnEnterWorld() { Sequence = ProtectionSequence = 0; BatchEnabled = ProtectionEnabled = false; }
    public override void UpdateDead() {
        BatchEnabled = false;
        if (Main.netMode != NetmodeID.MultiplayerClient) GatheringSystem.Cancel(Player.whoAmI);
    }
}
