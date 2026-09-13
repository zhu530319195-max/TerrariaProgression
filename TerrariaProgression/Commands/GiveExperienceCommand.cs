using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TerrariaProgression.Config;
using TerrariaProgression.Core;
using TerrariaProgression.Players;
using TerrariaProgression.UI;

namespace TerrariaProgression.Commands;

public sealed class GiveExperienceCommand : ModCommand
{
    public override string Command => "tpgivexp";
    public override CommandType Type => CommandType.Chat | CommandType.Console;
    public override string Usage => "/tpgivexp <XP> (server console: tpgivexp <slot> <XP>)";
    public override string Description => "开发测试：添加经验 / Developer XP test";
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (!ModContent.GetInstance<ProgressionConfig>().EnableDebugCommands)
            throw new UsageException(Language.GetTextValue("Mods.TerrariaProgression.Messages.DebugDisabled"));
        if (Main.netMode == NetmodeID.MultiplayerClient || (Main.netMode == NetmodeID.Server && caller.CommandType != CommandType.Console))
            throw new UsageException(Language.GetTextValue("Mods.TerrariaProgression.Messages.ConsoleOnly"));
        bool console = caller.CommandType == CommandType.Console;
        if (args.Length != (console ? 2 : 1) || !Experience.TryParse(args[^1], out var xp) || xp <= 0)
            throw new UsageException(Usage);
        Player target = caller.Player;
        if (console) {
            if (!int.TryParse(args[0], out int slot) || slot < 0 || slot >= Main.maxPlayers || !Main.player[slot].active) throw new UsageException(Usage);
            target = Main.player[slot];
        }
        var player = target.GetModPlayer<ProgressionPlayer>();
        if (!player.SessionReady) throw new UsageException(Language.GetTextValue("Mods.TerrariaProgression.Messages.Waiting"));
        player.Award(xp);
        caller.Reply(StatusText.For(player));
    }
}
