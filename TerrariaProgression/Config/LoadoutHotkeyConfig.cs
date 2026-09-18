using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using Terraria.ModLoader.Config;
using TerrariaProgression.UI;

namespace TerrariaProgression.Config;

public sealed class LoadoutHotkeyConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;
    [DefaultValue(true)] public bool Enabled = true;
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<LoadoutShortcut> Shortcuts = Defaults();
    private static List<LoadoutShortcut> Defaults()
    {
        var result=new List<LoadoutShortcut>();
        for(int i=1;i<=10;i++) result.Add(new() { LoadoutNumber=i, Shortcut="Shift+"+(i%10) });
        return result;
    }
    public override void OnChanged()
    {
        LoadoutHotkeySystem.Configure(this);
        if(LoadoutHotkeySystem.InvalidEntries.Count>0&&!Terraria.Main.dedServ&&!Terraria.Main.gameMenu)
            Terraria.Main.NewText(Terraria.Localization.Language.GetTextValue("Mods.TerrariaProgression.TalentsUI.InvalidShortcuts",string.Join(", ",LoadoutHotkeySystem.InvalidEntries)));
    }
}

public sealed class LoadoutShortcut
{
    [DefaultValue(1), Range(1, 2300)] public int LoadoutNumber = 1;
    [DefaultValue("Shift+1")] public string Shortcut = "Shift+1";
    public override string ToString() => LoadoutNumber+": "+Shortcut;
}
