using System;
using System.Collections;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using TerrariaProgression.Players;
using TerrariaProgression.UI;

namespace ProgressionHarness;

public sealed partial class RuntimeChecks
{
    private void RunMenuLifecycle()
    {
        Reset();
        var player = Main.LocalPlayer;
        var playersField = typeof(Player).GetField("modPlayers", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var components = playersField.GetValue(player);
        bool oldMenu = Main.gameMenu, oldServer = Main.dedServ;
        var system = new TalentUISystem();
        typeof(ModType).GetProperty(nameof(ModType.Mod))!.SetValue(system,
            ModContent.GetInstance<TerrariaProgression.TerrariaProgression>());
        // This client-only system is deliberately instantiated by the server
        // harness. No graphics/font assets or player components are available.
        var bindings = (IDictionary)typeof(KeybindLoader).GetField("modKeybinds", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        const string key = "TerrariaProgression/ToggleTalents";
        var oldBinding = bindings[key];
        var panelField = typeof(TalentUISystem).GetField("panel", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var interfaceField = typeof(TalentUISystem).GetField("userInterface", BindingFlags.Instance | BindingFlags.NonPublic)!;
        try {
            Main.dedServ = false;
            Main.gameMenu = true;
            playersField.SetValue(player, Array.Empty<ModPlayer>());
            bool reproduced = false;
            try { player.GetModPlayer<ProgressionPlayer>(); }
            catch (IndexOutOfRangeException) { reproduced = true; }
            Check(reproduced, "reproduce 0.8.0 premature player access failure with an empty native component table");
            system.Load();
            Check(TalentUISystem.ToggleKey != null && panelField.GetValue(system) == null && interfaceField.GetValue(system) == null,
                "client Load registers keybind without creating or activating widgets before player setup");
            Check(!TalentUISystem.CanUseMenu, "main menu refuses talent access before player initialization");
            Main.gameMenu = false;
            Check(!TalentUISystem.CanUseMenu, "incomplete local player safely refuses talent access even outside main menu");
            TalentUISystem.Toggle();
            system.UpdateUI(new GameTime());
            Check(!TalentUISystem.Visible && panelField.GetValue(system) == null,
                "early toggle and update do not instantiate the menu or access missing components");
            playersField.SetValue(player, components);
            Check(TalentUISystem.CanUseMenu, "initialized living client player is eligible to open talents");
            player.dead = true;
            Check(!TalentUISystem.CanUseMenu, "dead player cannot open talents");
            player.dead = false;
            Main.dedServ = true;
            Check(!TalentUISystem.CanUseMenu, "dedicated server never opens client talents");
            system.OnWorldUnload();
            system.Unload();
            Check(!TalentUISystem.Visible && TalentUISystem.ToggleKey == null && panelField.GetValue(system) == null,
                "world exit and unload are safe when the menu was never opened");
        }
        finally {
            system.Unload();
            if (oldBinding == null) bindings.Remove(key); else bindings[key] = oldBinding;
            playersField.SetValue(player, components);
            Main.gameMenu = oldMenu;
            Main.dedServ = oldServer;
            Reset();
        }
    }
}
