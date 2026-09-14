using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace TerrariaProgression.Talents;

public sealed class ToolPowerSystem : ModSystem
{
    private static Item? scopedItem;
    private static Player? picking;
    private static int originalAxe;
    public override void Load()
    {
        On_Player.PickTile += Pick;
        On_Player.GetPickaxeDamage += PickDamage;
        On_Player.ItemCheck_UseMiningTools_ActuallyUseMiningTool += Tool;
    }
    public override void Unload()
    {
        On_Player.PickTile -= Pick;
        On_Player.GetPickaxeDamage -= PickDamage;
        On_Player.ItemCheck_UseMiningTools_ActuallyUseMiningTool -= Tool;
        scopedItem = null; picking = null;
    }
    internal static int AxePower(Player p, Item item) => ToolPowerRules.Axe(ReferenceEquals(item, scopedItem) ? originalAxe : item.axe, ExtendedTalentPlayer.Level(p, "AxePower"));
    private static void Pick(On_Player.orig_PickTile orig, Player p, int x, int y, int power)
    {
        var old = picking; picking = p;
        try { orig(p, x, y, ToolPowerRules.Pick(power, ExtendedTalentPlayer.Level(p, "PickPower"))); }
        finally { picking = old; }
    }
    private static int PickDamage(On_Player.orig_GetPickaxeDamage orig, Player p, int x, int y, int power, int buffer, Tile tile) =>
        orig(p, x, y, picking == p ? power : ToolPowerRules.Pick(power, ExtendedTalentPlayer.Level(p, "PickPower")), buffer, tile);
    private static void Tool(On_Player.orig_ItemCheck_UseMiningTools_ActuallyUseMiningTool orig, Player p, Item item, out bool walls, int x, int y)
    {
        int effective = AxePower(p, item), oldAxe = item.axe, oldOriginal = originalAxe;
        var oldItem = scopedItem;
        originalAxe = ReferenceEquals(item, scopedItem) ? originalAxe : oldAxe;
        scopedItem = item; item.axe = effective;
        try { orig(p, item, out walls, x, y); }
        finally { item.axe = oldAxe; scopedItem = oldItem; originalAxe = oldOriginal; }
    }
}

public sealed class ToolPowerItem : GlobalItem
{
    public override void ModifyTooltips(Item item, List<TooltipLine> lines)
    {
        if (Main.gameMenu || Main.LocalPlayer == null) return;
        int pick = ToolPowerRules.Pick(item.pick, ExtendedTalentPlayer.Level(Main.LocalPlayer, "PickPower"));
        int axe = ToolPowerSystem.AxePower(Main.LocalPlayer, item);
        if (pick != item.pick) lines.Add(new TooltipLine(Mod, "TalentPickPower", Language.GetTextValue("Mods.TerrariaProgression.Messages.EffectivePickPower", item.pick, pick)));
        if (axe != item.axe) lines.Add(new TooltipLine(Mod, "TalentAxePower", Language.GetTextValue("Mods.TerrariaProgression.Messages.EffectiveAxePower", item.axe * 5L, axe * 5L)));
    }
}
