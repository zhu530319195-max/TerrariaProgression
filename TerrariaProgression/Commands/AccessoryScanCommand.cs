using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Config;
using TerrariaProgression.Diagnostics;

namespace TerrariaProgression.Commands;

public sealed class AccessoryScanCommand : ModCommand
{
    public override string Command => "tpscanaccessories";
    public override CommandType Type => CommandType.Chat | CommandType.Console;
    public override string Usage => "/tpscanaccessories";
    public override string Description => "开发工具：导出饰品映射与兼容候选报告 / Export passive accessory report";
    public override void Action(CommandCaller caller,string input,string[] args)
    {
        if(args.Length!=0)throw new UsageException(Usage);
        if(!ModContent.GetInstance<ProgressionConfig>().EnableDebugCommands || Main.netMode==NetmodeID.MultiplayerClient || (Main.netMode==NetmodeID.Server && caller.CommandType!=CommandType.Console))
            throw new UsageException("仅限启用开发命令后的单人游戏或服务器控制台 / Singleplayer or server console with developer commands enabled.");
        string stem=AccessoryTalentScanner.WriteReport();
        caller.Reply("饰品报告已导出（仅候选，未执行饰品效果） / Candidate reports: "+stem+".json / .csv");
    }
}
