using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace TerrariaProgression.UI;

[Autoload(Side = ModSide.Client)]
public sealed class TalentUISystem : ModSystem
{
    internal static ModKeybind? ToggleKey;
    private UserInterface? userInterface;
    private TalentUIState? panel;
    private GameTime lastTime = new();
    internal static bool Visible;
    public override void Load()
    {
        ToggleKey = KeybindLoader.RegisterKeybind(Mod, "ToggleTalents", "P");
        userInterface = new UserInterface();
        panel = new TalentUIState();
        panel.Activate();
    }
    public override void Unload() { ToggleKey = null; Visible = false; userInterface = null; panel = null; }
    public override void OnWorldUnload() { Visible = false; userInterface?.SetState(null); }
    internal static void Toggle()
    {
        if (Main.gameMenu || Main.dedServ) return;
        Visible = !Visible;
        var system = ModContent.GetInstance<TalentUISystem>();
        system.userInterface?.SetState(Visible ? system.panel : null);
    }
    public override void UpdateUI(GameTime gameTime)
    {
        lastTime = gameTime;
        if (Main.gameMenu || Main.LocalPlayer.dead) { Visible = false; userInterface?.SetState(null); }
        if (Visible) userInterface?.Update(gameTime);
    }
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
        if (index < 0) index = layers.Count;
        layers.Insert(index, new LegacyGameInterfaceLayer("TerrariaProgression: Talents", () => {
            if (Visible && !Main.gameMenu) userInterface?.Draw(Main.spriteBatch, lastTime);
            return true;
        }, InterfaceScaleType.UI));
    }
}
