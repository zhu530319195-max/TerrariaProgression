using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Config;

namespace TerrariaProgression.Commands;

public sealed class LootTestCommand : ModCommand
{
    public override string Command => "tplootsample";
    public override CommandType Type => CommandType.Chat;
    public override string Usage => "/tplootsample";
    public override string Description => "单人开发测试：领取测试袋、宝匣和武器 / Singleplayer test supplies";
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (Main.netMode != NetmodeID.SinglePlayer || !ModContent.GetInstance<ProgressionConfig>().EnableDebugCommands)
            throw new UsageException("仅限单人且须开启开发测试命令 / Enable debug commands in singleplayer.");
        if (args.Length != 0) throw new UsageException(Usage);
        var source = caller.Player.GetSource_Misc("TerrariaProgression/TestSupplies");
        caller.Player.QuickSpawnItem(source, ItemID.KingSlimeBossBag, 10);
        caller.Player.QuickSpawnItem(source, ItemID.WoodenCrate, 10);
        foreach (int type in new[] { ItemID.CopperShortsword, ItemID.Spear, ItemID.Minishark, ItemID.WoodenBow, ItemID.WandofSparking })
            caller.Player.QuickSpawnItem(source, type);
        caller.Player.QuickSpawnItem(source, ItemID.MusketBall, 500);
        caller.Player.QuickSpawnItem(source, ItemID.WoodenArrow, 500);
        caller.Reply("测试物资已生成。按 P 开关天赋，对比开袋产量、抽取次数、攻击范围与射速。 / Test supplies spawned.");
    }
}
