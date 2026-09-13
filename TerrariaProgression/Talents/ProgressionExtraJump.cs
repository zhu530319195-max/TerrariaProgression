using Terraria;
using Terraria.ModLoader;
using TerrariaProgression.Players;

namespace TerrariaProgression.Talents;

// One reusable native jump, O(1) state even with enormous talent levels.
public sealed class ProgressionExtraJump : ExtraJump
{
    public override Position GetDefaultPosition() => new After(CloudInABottle);
    public override float GetDurationMultiplier(Player player) => .75f; // Native cloud-jump duration.
    public override bool CanStart(Player player)
    {
        var p = player.GetModPlayer<FunctionalTalentPlayer>();
        return p.UsedJumps < p.MaximumJumps;
    }
    public override void OnStarted(Player player, ref bool playSound)
    {
        player.GetModPlayer<FunctionalTalentPlayer>().UsedJumps++;
        // Availability is reusable; CanStart enforces the remaining count. Toggling
        // or upgrading in the air never resets UsedJumps.
        player.GetJumpState<ProgressionExtraJump>().Available = true;
    }
    public override void OnRefreshed(Player player) => player.GetModPlayer<FunctionalTalentPlayer>().UsedJumps = 0;
}
