using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using TerrariaProgression.Config;
using TerrariaProgression.Core;
using TerrariaProgression.Players;

namespace TerrariaProgression.UI;

[Autoload(Side = ModSide.Client)]
public sealed class LoadoutHotkeySystem : ModSystem
{
    private static readonly List<(int Number,LoadoutChord Chord)> bindings = new();
    private KeyboardState previous;
    private static Guid? requested;
    internal static bool Enabled;
    internal static readonly List<int> InvalidEntries = new();
    internal static void Configure(LoadoutHotkeyConfig config)
    {
        bindings.Clear();InvalidEntries.Clear();Enabled=config.Enabled;
        if(config.Shortcuts==null)return;
        for(int i=0;i<config.Shortcuts.Count;i++) {
            var entry=config.Shortcuts[i];
            if(entry!=null&&string.IsNullOrWhiteSpace(entry.Shortcut))continue;
            if(entry==null||entry.LoadoutNumber<1||entry.LoadoutNumber>2300||!LoadoutChord.TryParse(entry.Shortcut,out var chord)) {InvalidEntries.Add(i+1);continue;}
            bindings.Add((entry.LoadoutNumber,chord));
        }
    }
    public override void Load() => Configure(ModContent.GetInstance<LoadoutHotkeyConfig>());
    public override void Unload() {bindings.Clear();InvalidEntries.Clear();requested=null;Enabled=false;}
    public override void OnWorldUnload() {requested=null;previous=default;}
    internal static bool ContextAllowed => !Main.dedServ && Main.hasFocus && !Main.gameMenu && !Main.gamePaused
        && !Main.ingameOptionsWindow && !Main.inFancyUI && !Main.drawingPlayerChat && !Main.editSign && !Main.editChest
        && !Main.blockInput && !PlayerInput.WritingText && Main.CurrentInputTextTakerOverride==null
        && !TalentUISystem.IsTyping && !TalentUISystem.HasLoadoutOverlay
        && Main.LocalPlayer.active && !Main.LocalPlayer.dead;
    public override void PostUpdateInput() => Process(Keyboard.GetState());
    internal void Process(KeyboardState now)
    {
        var old=previous;previous=now; // Always consume edges, even while typing/paused.
        if(!ContextAllowed) return;
        var player=Main.LocalPlayer.GetModPlayer<ProgressionPlayer>();
        if(requested is Guid target&&!player.RequestPending) {
            if(player.State.ActiveLoadoutId==target) Main.NewText(TalentUIState.Text("LoadoutSwitched",player.State.ActiveLoadout.Name));
            else if(player.HasResult) Main.NewText(TalentUIState.Text("Result"+player.LastResult));
            requested=null;
        }
        HandleBindings(now,old,player);
    }
    internal static bool HandleBindings(KeyboardState now,KeyboardState old,ProgressionPlayer player)
    {
        if(!Enabled||!player.SessionReady)return false;
        foreach(var binding in bindings) {
            if(!binding.Chord.Held(now))continue;
            SuppressHotbar(binding.Chord.Key);
            // Deterministic first binding wins; never send two requests.
            return binding.Chord.Pressed(now,old) && TrySwitch(player,binding.Number);
        }
        return false;
    }
    internal static bool TrySwitch(ProgressionPlayer player,int number)
    {
        if(!player.SessionReady||player.Player.dead||player.RequestPending||player.RequestCoolingDown
            ||number<1||number>player.State.Loadouts.Count)return false;
        Guid id=player.State.Loadouts[number-1].Id;
        if(id==player.State.ActiveLoadoutId)return false;
        player.RequestTalent(TalentOperation.ActivateLoadout,id.ToString("N"));
        requested=id;
        return true;
    }
    internal static void SuppressHotbar(Keys key)
    {
        if(PlayerInput.CurrentProfile==null||!PlayerInput.CurrentProfile.InputModes.TryGetValue(InputMode.Keyboard,out var mapping))return;
        for(int i=1;i<=10;i++) {
            string trigger="Hotbar"+i;
            if(!mapping.KeyStatus.TryGetValue(trigger,out var keys)||!keys.Contains(key.ToString()))continue;
            PlayerInput.Triggers.Current.KeyStatus[trigger]=false;
            PlayerInput.Triggers.JustPressed.KeyStatus[trigger]=false;
        }
    }
}
