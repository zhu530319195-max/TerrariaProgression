using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using TerrariaProgression.Core;

namespace TerrariaProgression.UI;

[Autoload(Side = ModSide.Client)]
public sealed class DefaultKeybindSystem : ModSystem
{
    private HashSet<string> migrated = new(StringComparer.Ordinal);
    private bool stopped;
    private static string MarkerPath => Path.Combine(Main.SavePath, "ModConfigs", "TerrariaProgression.KeybindDefaults.v1.json");
    public override void Load()
    {
        stopped = false;
        try {
            migrated = File.Exists(MarkerPath) ? JsonConvert.DeserializeObject<HashSet<string>>(File.ReadAllText(MarkerPath)) ?? new() : new();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) {
            stopped = true; Mod.Logger.Warn("Cannot read keybind migration marker; existing input configuration preserved.", e);
        }
    }
    public override void PostUpdateInput()
    {
        if (stopped || PlayerInput.Profiles.Count == 0) return;
        bool changed = false;
        foreach (var pair in PlayerInput.Profiles)
            foreach (var mode in new[] { InputMode.Keyboard, InputMode.KeyboardUI })
                if (pair.Value.InputModes.TryGetValue(mode, out var keys))
                    changed |= DefaultKeybindRules.Initialize(pair.Key + "/" + mode, keys.KeyStatus, migrated);
        if (!changed) return;
        try {
            if (!PlayerInput.Save()) throw new IOException("Input profile save failed.");
            Directory.CreateDirectory(Path.GetDirectoryName(MarkerPath)!);
            File.WriteAllText(MarkerPath + ".tmp", JsonConvert.SerializeObject(migrated));
            File.Move(MarkerPath + ".tmp", MarkerPath, true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
            stopped = true; Mod.Logger.Warn("Default keys are active for this session, but their migration marker could not be saved.", e);
        }
    }
}
