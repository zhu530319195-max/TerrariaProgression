using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader.Config.UI;

namespace TerrariaProgression.Config;

public sealed class RestoreTalentLimitsElement : ConfigElement
{
    public override void OnBind()
    {
        base.OnBind();
        Height.Set(42, 0);
        DrawLabel = false;
        var button = new UITextPanel<string>(Language.GetTextValue("Mods.TerrariaProgression.TalentsUI.RestoreTalentLimits"), .8f);
        button.Width.Set(-8, 1); button.Height.Set(34, 0); button.Left.Set(4, 0); button.Top.Set(4, 0);
        button.OnLeftClick += (_, _) => SetObject(true);
        Append(button);
    }
}
