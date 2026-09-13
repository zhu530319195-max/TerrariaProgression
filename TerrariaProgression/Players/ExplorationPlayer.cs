using System;
using Terraria;
using Terraria.ModLoader;
using TerrariaProgression.Core;

namespace TerrariaProgression.Players;

public sealed class ExplorationPlayer : ModPlayer
{
    private bool hadFlightBonus;
    internal static double Factor(Player p, string id, double perLevel) => 1 + perLevel * TalentMath.Level(ExtendedTalentPlayer.Level(p,id));
    internal static bool HasWings(Player p) => p.wingsLogic>0 && p.equippedWings is { IsAir: false } && !p.mount.Active && !p.dead;
    internal bool EmitsLight => !Player.dead && Player.active && ExtendedTalentPlayer.Level(Player,"SelfLight")>0;
    public override void UpdateEquips()
    {
        if (ExtendedTalentPlayer.Level(Player,"NightVision")>0) Player.nightVision=true;
        if (ExtendedTalentPlayer.Level(Player,"DangerSense")>0) Player.dangerSense=true;
    }
    public override void PostUpdateEquips()
    {
        var level=ExtendedTalentPlayer.Level(Player,"FlightTime");
        bool enabled=HasWings(Player) && Player.wingTimeMax>0 && level>0;
        if (enabled) Player.wingTimeMax=Math.Min(int.MaxValue-4096,TalentMath.AddInt(Player.wingTimeMax,level*60));
        // Capacity only. Landing/real equipment refills fuel through the engine.
        if (enabled || hadFlightBonus) Player.wingTime=Math.Min(Player.wingTime,Player.wingTimeMax);
        hadFlightBonus=enabled;
    }
    public override void PostUpdate()
    {
        if (!Main.dedServ && EmitsLight) Lighting.AddLight(Player.Center,.8f,.95f,1f);
    }
}
