using System.Collections.Generic;

namespace TerrariaProgression.Core;

public static class DefaultKeybindRules
{
    public static readonly IReadOnlyDictionary<string, string> Defaults = new Dictionary<string, string> {
        ["TerrariaProgression/ToggleTalents"] = "P",
        ["TerrariaProgression/GatheringAction"] = "LeftAlt",
        ["TerrariaProgression/GatheringMode"] = "G",
        ["TerrariaProgression/MiningProtection"] = "K"
    };
    public static bool Initialize(string profile, IDictionary<string, List<string>> keys, ISet<string> migrated)
    {
        bool initial = migrated.Add(profile), protection = migrated.Add(profile + "/MiningProtection-v1");
        if (!initial && !protection) return false;
        foreach (var pair in Defaults) {
            if (pair.Key.EndsWith("/MiningProtection") ? !protection : !initial) continue;
            if (!keys.TryGetValue(pair.Key, out var list)) keys[pair.Key] = list = new();
            if (list.Count == 0) list.Add(pair.Value);
        }
        return true;
    }
}
