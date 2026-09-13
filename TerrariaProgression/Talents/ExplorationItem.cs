using System;
using Terraria;
using Terraria.ModLoader;
using TerrariaProgression.Players;

namespace TerrariaProgression.Talents;

public sealed class ExplorationItem : GlobalItem
{
    public override float UseSpeedMultiplier(Item item, Player player)
    {
        // Placeable metadata only; never accelerate weapons, pickaxes or hammers.
        if (item.damage>0 || item.pick>0 || item.axe>0 || item.hammer>0) return 1;
        string? id=item.createWall>0 ? "WallPlacementSpeed" : item.createTile>-1 ? "PlacementSpeed" : null;
        // Native timers cannot run faster than one tick; keep the input finite.
        return id==null ? 1 : (float)Math.Min(1000000,ExplorationPlayer.Factor(player,id,.2));
    }
    internal static float Scale(float value, double factor) => (float)Math.Clamp(value*factor,0,256);
    public override void HorizontalWingSpeeds(Item item, Player player, ref float speed, ref float acceleration)
    {
        if (!ExplorationPlayer.HasWings(player)) return;
        double factor=ExplorationPlayer.Factor(player,"FlightSpeed",.05);
        if (factor<=1) return;
        speed=Scale(speed,factor); acceleration=Scale(acceleration,factor);
    }
    public override void VerticalWingSpeeds(Item item, Player player, ref float ascentWhenFalling,
        ref float ascentWhenRising, ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend)
    {
        if (!ExplorationPlayer.HasWings(player)) return;
        double factor=ExplorationPlayer.Factor(player,"FlightSpeed",.05);
        if (factor<=1) return;
        ascentWhenFalling=Scale(ascentWhenFalling,factor); ascentWhenRising=Scale(ascentWhenRising,factor);
        maxCanAscendMultiplier=Scale(maxCanAscendMultiplier,factor); maxAscentMultiplier=Scale(maxAscentMultiplier,factor);
        constantAscend=Scale(constantAscend,factor);
    }
}
