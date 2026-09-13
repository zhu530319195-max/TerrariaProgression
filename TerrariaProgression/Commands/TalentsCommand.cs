using Terraria.ModLoader;
using TerrariaProgression.UI;

namespace TerrariaProgression.Commands;

public sealed class TalentsCommand : ModCommand
{
    public override string Command => "tptalents";
    public override CommandType Type => CommandType.Chat;
    public override string Description => "Open the progression talent panel / 打开天赋页面";
    public override void Action(CommandCaller caller, string input, string[] args) => TalentUISystem.Toggle();
}
