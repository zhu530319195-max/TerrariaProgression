using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace TerrariaProgression.Commands;
public sealed class GatheringCommand : ModCommand
{
    public override string Command => "tpgather";
    public override CommandType Type => CommandType.Chat;
    public override string Usage => "/tpgather area|vein|toggle|protect|coords";
    public override string Description => "切换范围／矿脉采集模式，或查看鼠标方块坐标";
    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (args.Length != 1) { caller.Reply(Usage); return; }
        if (args[0] == "toggle") { caller.Player.GetModPlayer<GatheringPlayer>().ToggleBatch(); return; }
        if (args[0] == "protect") { caller.Player.GetModPlayer<GatheringPlayer>().ToggleProtection(); return; }
        if (args[0] == "coords") { caller.Reply($"X={Player.tileTargetX}, Y={Player.tileTargetY}"); return; }
        if (args[0] != "area" && args[0] != "vein") { caller.Reply(Usage); return; }
        var p = caller.Player.GetModPlayer<GatheringPlayer>();
        p.MiningMode = args[0] == "area" ? GatheringMode.Area : GatheringMode.Vein;
        caller.Reply(GatheringPlayer.ModeName(p.MiningMode), Color.Goldenrod);
    }
}
