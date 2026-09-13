using Terraria;
using Terraria.ModLoader;
using TerrariaProgression.Players;

namespace TerrariaProgression.Talents;

public sealed class FunctionalTalentSystem : ModSystem
{
    public override void Load()
    {
        On_Player.DashMovement += Dash;
        On_Player.WallslideMovement += Wall;
        On_Player.WingMovement += Flight;
        On_Player.CheckDrowning += Drowning;
        On_Player.RefreshInfoAccsFromTeamPlayers += Information;
    }
    public override void Unload()
    {
        On_Player.DashMovement -= Dash;
        On_Player.WallslideMovement -= Wall;
        On_Player.WingMovement -= Flight;
        On_Player.CheckDrowning -= Drowning;
        On_Player.RefreshInfoAccsFromTeamPlayers -= Information;
    }
    private static void Dash(On_Player.orig_DashMovement orig, Player player)
    {
        // Apply at the native consumer, after all equipment/mod equip hooks.
        // Existing vanilla or mod dash identifiers always win. Never reset cooldowns.
        int equipment = player.dashType;
        if (equipment == 0 && player.GetModPlayer<FunctionalTalentPlayer>().Has("Dash")) player.dashType = 1;
        try { orig(player); }
        finally { player.dashType = equipment; }
    }
    private static void Wall(On_Player.orig_WallslideMovement orig, Player player)
    {
        int equipment = player.spikedBoots;
        var functional = player.GetModPlayer<FunctionalTalentPlayer>();
        // Native tier 2 grips the wall and permits repeated wall jumps; tier 1 slides.
        int talent = functional.Has("WallClimb") ? 2 : functional.Has("WallSlide") ? 1 : 0;
        player.spikedBoots = System.Math.Max(equipment, talent);
        try { orig(player); }
        finally { player.spikedBoots = equipment; }
    }
    private static void Flight(On_Player.orig_WingMovement orig, Player player)
    {
        bool conserve = player.wingsLogic > 0 && !player.mount.Active && !player.dead &&
            player.GetModPlayer<FunctionalTalentPlayer>().Has("UnlimitedFlight");
        float before = player.wingTime;
        orig(player);
        // Conserve existing wing fuel without granting wings, speed or a refill.
        // Native grounding/grappling and a real Soaring Insignia may still refill it.
        if (conserve && before > 0 && float.IsFinite(before))
            player.wingTime = System.Math.Max(before, player.wingTime);
    }
    private static void Drowning(On_Player.orig_CheckDrowning orig, Player player)
    {
        // Gills in For the Worthy reverses breathing and can drown players in air.
        // Suspend only drowning while this independent ability is enabled.
        if (player.GetModPlayer<FunctionalTalentPlayer>().Has("UnderwaterBreathing")) return;
        orig(player);
    }
    private static void Information(On_Player.orig_RefreshInfoAccsFromTeamPlayers orig, Player player)
    {
        orig(player);
        // UI refresh can rebuild info flags outside the ordinary equip update.
        player.GetModPlayer<FunctionalTalentPlayer>().ApplyInformation();
    }
}
