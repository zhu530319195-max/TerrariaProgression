using Terraria;
using Terraria.ModLoader;
using TerrariaProgression.Players;

namespace TerrariaProgression.Talents;

public sealed class FunctionalTalentSystem : ModSystem
{
    public override void Load()
    {
        On_Player.CheckDrowning += Drowning;
        On_Player.RefreshInfoAccsFromTeamPlayers += Information;
    }
    public override void Unload()
    {
        On_Player.CheckDrowning -= Drowning;
        On_Player.RefreshInfoAccsFromTeamPlayers -= Information;
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
