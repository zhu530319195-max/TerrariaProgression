using System;
using System.Numerics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerrariaProgression.Core;
using TerrariaProgression.Talents;

namespace TerrariaProgression.Players;

public sealed class FunctionalTalentPlayer : ModPlayer
{
    internal BigInteger UsedJumps;
    internal bool Has(string id) => ExtendedTalentPlayer.Level(Player, id) > 0;
    internal BigInteger MaximumJumps => ExtendedTalentPlayer.Level(Player, "MultiJump");
    public override void Initialize() => UsedJumps = 0;
    public override void OnRespawn() => UsedJumps = 0;
    public override void UpdateEquips()
    {
        // Only provide purchased effects; never clear real equipment/potion flags.
        if (Has("NoFallDamage")) Player.noFallDmg = true;
        if (Has("AutoJump")) Player.autoJump = true;
        if (Has("KnockbackImmunity")) Player.noKnockback = true;
        if (Has("WaterWalking")) Player.waterWalk2 = true;
        if (Has("LavaImmunity")) Player.lavaImmune = true;
        if (Has("HotTileImmunity")) Player.fireWalk = true;
        if (MaximumJumps > 0) Player.GetJumpState<ProgressionExtraJump>().Enable();
        ApplyInformation();
    }
    public override void PreUpdateMovement()
    {
        // Vanilla waterWalk includes both water and lava. Enable it only near a
        // lava surface so buying lava walking does not also grant water walking.
        if (Has("LavaWalking") && LavaAtFeet(Player)) Player.waterWalk = true;
    }
    internal static bool LavaAtFeet(Player p)
    {
        if (p.gravDir != 1) return false;
        int left = Math.Clamp((int)(p.position.X / 16), 1, Main.maxTilesX - 2);
        int right = Math.Clamp((int)((p.position.X + p.width) / 16), 1, Main.maxTilesX - 2);
        int top = Math.Clamp((int)((p.position.Y + p.height - 8) / 16), 1, Main.maxTilesY - 2);
        int bottom = Math.Clamp((int)((p.position.Y + p.height + Math.Max(16, p.velocity.Y)) / 16), top, Main.maxTilesY - 2);
        for (int x = left; x <= right; x++)
            for (int y = top; y <= bottom; y++)
                if (Main.tile[x,y].LiquidAmount > 0 && Main.tile[x,y].LiquidType == LiquidID.Lava) return true;
        return false;
    }
    internal void ApplyInformation()
    {
        var progression = Player.GetModPlayer<ProgressionPlayer>();
        if (!progression.SessionReady) return;
        bool On(string child) => FunctionalTalentRegistry.ChildEnabled(progression.State, "AllInformation", child);
        if (On("Time")) Player.accWatch = Math.Max(Player.accWatch, 3);
        if (On("Depth")) Player.accDepthMeter = Math.Max(Player.accDepthMeter, 1);
        if (On("Compass")) Player.accCompass = Math.Max(Player.accCompass, 1);
        if (On("Weather")) Player.accWeatherRadio = true;
        if (On("Moon")) Player.accCalendar = true;
        if (On("Fishing")) Player.accFishFinder = true;
        if (On("Radar")) Player.accThirdEye = true;
        if (On("Dps")) Player.accDreamCatcher = true;
        if (On("RareCreatures")) Player.accCritterGuide = true;
        if (On("Ore")) Player.accOreFinder = true;
        if (On("Treasure")) Player.findTreasure = true;
        if (On("KillCount")) Player.accJarOfSouls = true;
    }
}
