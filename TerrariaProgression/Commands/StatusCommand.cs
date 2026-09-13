using Terraria;
using Terraria.ModLoader;
using TerrariaProgression.Players;
using TerrariaProgression.UI;

namespace TerrariaProgression.Commands;

public sealed class StatusCommand : ModCommand
{
    public override string Command => "tpstatus";
    public override CommandType Type => CommandType.Chat | CommandType.Console;
    public override string Usage => "/tpstatus (console: tpstatus <player slot>)";
    public override string Description => "查看等级、经验和天赋点 / Show progression";
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (caller.CommandType == CommandType.Console) {
            if (args.Length != 1 || !int.TryParse(args[0], out int slot) || slot < 0 || slot >= Main.maxPlayers || !Main.player[slot].active)
                throw new UsageException(Usage);
            caller.Reply(StatusText.For(Main.player[slot].GetModPlayer<ProgressionPlayer>()));
        }
        else caller.Reply(StatusText.For(caller.Player.GetModPlayer<ProgressionPlayer>()));
    }
}
