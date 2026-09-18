using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using TerrariaProgression.Players;

namespace TerrariaProgression.UI;

[Autoload(Side = ModSide.Client)]
public sealed class TalentUISystem : ModSystem
{
    internal static ModKeybind? ToggleKey;
    private UserInterface? userInterface;
    private TalentUIState? panel;
    private GameTime lastTime = new();
    internal static bool Visible;
    internal static bool IsTyping => Visible && ModContent.GetInstance<TalentUISystem>().panel?.IsTyping == true;
    internal static bool HasLoadoutOverlay => Visible && ModContent.GetInstance<TalentUISystem>().panel?.HasLoadoutOverlay == true;
    internal static bool CanUseMenu => !Main.gameMenu && !Main.dedServ && !Main.LocalPlayer.dead
        && Main.LocalPlayer.TryGetModPlayer<ProgressionPlayer>(out _);
    public override void Load()
    {
        ToggleKey = KeybindLoader.RegisterKeybind(Mod, "ToggleTalents", "P");
        // Load runs before the local player's ModPlayer array is populated.
        // Create and activate widgets only on the first in-world open.
    }
    public override void Unload() { panel?.EndSearch(); ToggleKey = null; Visible = false; userInterface = null; panel = null; }
    public override void OnWorldUnload() { panel?.EndSearch(); Visible = false; userInterface?.SetState(null); }
    internal static void Toggle()
    {
        if (!CanUseMenu) return;
        var system = ModContent.GetInstance<TalentUISystem>();
        if (!Visible) {
            system.userInterface ??= new UserInterface();
            system.panel ??= new TalentUIState();
        }
        Visible = !Visible;
        if (!Visible) system.panel?.EndSearch();
        system.userInterface?.SetState(Visible ? system.panel : null);
    }
    public override void PostUpdateInput() { if (Visible && CanUseMenu) panel?.MaintainTextFocus(); }
    public override void UpdateUI(GameTime gameTime)
    {
        lastTime = gameTime;
        if (!CanUseMenu) { panel?.EndSearch(); Visible = false; userInterface?.SetState(null); }
        if (Visible) userInterface?.Update(gameTime);
    }
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
        if (index < 0) index = layers.Count;
        layers.Insert(index, new LegacyGameInterfaceLayer("TerrariaProgression: Talents", () => {
            if (Visible && CanUseMenu) userInterface?.Draw(Main.spriteBatch, lastTime);
            return true;
        }, InterfaceScaleType.UI));
    }
}
