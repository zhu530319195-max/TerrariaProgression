using System;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerrariaProgression.Core;
using TerrariaProgression.Talents;
using TerrariaProgression.UI;

namespace TerrariaProgression.Players;

public sealed class GatheringPlayer : ModPlayer
{
    public GatheringMode MiningMode = GatheringMode.Area;
    internal uint Sequence;
    internal static bool InputAllowed => !Main.gameMenu && !Main.drawingPlayerChat && !Main.editSign && !Main.editChest && !TalentUISystem.Visible && !Main.LocalPlayer.mouseInterface;
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        if (!InputAllowed || Player.dead) return;
        if (GatheringSystem.ModeKey?.JustPressed == true) {
            MiningMode = MiningMode == GatheringMode.Area ? GatheringMode.Vein : GatheringMode.Area;
            Main.NewText(Terraria.Localization.Language.GetTextValue("Mods.TerrariaProgression.Messages.GatheringMode", ModeName(MiningMode)));
        }
    }
    internal static string ModeName(GatheringMode mode) => Terraria.Localization.Language.GetTextValue("Mods.TerrariaProgression.TalentNames." + GatheringRules.Talent(mode));
    public override void SaveData(TagCompound tag) => tag["MiningMode"] = (int)MiningMode;
    public override void LoadData(TagCompound tag) => MiningMode = tag.GetInt("MiningMode") == 1 ? GatheringMode.Vein : GatheringMode.Area;
    public override void OnEnterWorld() => Sequence = 0;
}
