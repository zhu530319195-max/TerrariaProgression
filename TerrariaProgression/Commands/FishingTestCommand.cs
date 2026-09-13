using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Config;

namespace TerrariaProgression.Commands;

public sealed class FishingTestCommand : ModCommand
{
    public override string Command => "tpfishsample";
    public override CommandType Type => CommandType.Chat;
    public override string Usage => "/tpfishsample";
    public override string Description => "单人开发测试：领取鱼竿、鱼饵和声呐药水 / Fishing test supplies";
    public override void Action(CommandCaller caller,string input,string[] args)
    {
        if(args.Length!=0)throw new UsageException(Usage);
        if(Main.netMode!=NetmodeID.SinglePlayer || !ModContent.GetInstance<ProgressionConfig>().EnableDebugCommands)
            throw new UsageException("仅限单人且须开启开发测试命令 / Enable developer commands in singleplayer.");
        var source=caller.Player.GetSource_Misc("TerrariaProgression/FishingTest");
        caller.Player.QuickSpawnItem(source,ItemID.GoldenFishingRod);
        caller.Player.QuickSpawnItem(source,ItemID.MasterBait,200);
        caller.Player.QuickSpawnItem(source,ItemID.SonarPotion,10);
        caller.Reply("钓鱼测试物资已生成。请到足够大的水池测试。 / Fishing supplies spawned; use a sufficiently large pond.");
    }
}
