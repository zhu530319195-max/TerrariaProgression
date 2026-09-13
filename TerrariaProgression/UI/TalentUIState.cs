using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using TerrariaProgression.Config;
using TerrariaProgression.Core;
using TerrariaProgression.Networking;
using TerrariaProgression.Players;

namespace TerrariaProgression.UI;

internal sealed class TalentUIState : UIState
{
    private readonly UIPanel panel = new();
    private readonly UIList list = new();
    private readonly UIText header = new("", .8f), totals = new("", .72f), title = new("", 1.05f);
    private readonly UIText detail = new("", .8f), hint = new("", .72f), feedback = new("", .7f);
    private TalentCategory category;
    private string selected = "MaxLife";
    private float lastWidth, lastHeight;
    private ProgressionPlayer Player => Main.LocalPlayer.GetModPlayer<ProgressionPlayer>();
    internal static string Text(string key, params object[] args) => Language.GetTextValue("Mods.TerrariaProgression.TalentsUI." + key, args);
    private static string Name(string id) => Language.GetTextValue("Mods.TerrariaProgression.TalentNames." + id);
    private bool CanAct => Player.SessionReady && !Player.RequestPending && !Player.RequestCoolingDown && !Main.LocalPlayer.dead;
    private bool HasSelected => Player.State.Talents.ContainsKey(selected);
    public override void OnInitialize()
    {
        panel.BackgroundColor = new Color(20, 29, 48, 248);
        panel.BorderColor = new Color(176, 146, 84);
        panel.SetPadding(16);
        panel.HAlign = panel.VAlign = .5f;
        Append(panel);
        Place(header, panel, 0, 3, 1, -100, 26);
        Place(totals, panel, 0, 37, 1, 0, 22);
        var close = Button("Close", () => TalentUISystem.Toggle());
        close.Left.Set(-80, 1); close.Top.Set(0, 0); close.Width.Set(80, 0); panel.Append(close);
        for (int i = 0; i < 6; i++) {
            var value = (TalentCategory)i;
            var tab = Button("Category" + i, () => SelectCategory(value));
            tab.Left.Set((i % 3) * 4, (i % 3) / 3f);
            tab.Top.Set(72 + i / 3 * 36, 0);
            tab.Width.Set(-8, 1 / 3f);
            panel.Append(tab);
        }
        var left = new UIElement();
        left.Top.Set(158, 0); left.Width.Set(-16, .34f); left.Height.Set(-232, 1); panel.Append(left);
        list.Width.Set(-22, 1); list.Height.Set(0, 1); list.ListPadding = 5; left.Append(list);
        var scroll = new UIScrollbar();
        scroll.Left.Set(-18, 1); scroll.Height.Set(0, 1); scroll.SetView(100, 1000); left.Append(scroll);
        list.SetScrollbar(scroll);
        var rightArea = new UIElement();
        rightArea.Left.Set(0, .34f); rightArea.Top.Set(158, 0); rightArea.Width.Set(0, .66f); rightArea.Height.Set(-232, 1); panel.Append(rightArea);
        var detailsList = new UIList(); detailsList.Width.Set(-22, 1); detailsList.Height.Set(0, 1); rightArea.Append(detailsList);
        var detailScroll = new UIScrollbar(); detailScroll.Left.Set(-18, 1); detailScroll.Height.Set(0, 1); detailScroll.SetView(100, 1000); rightArea.Append(detailScroll);
        detailsList.SetScrollbar(detailScroll);
        var right = new UIElement(); right.Width.Set(0, 1); right.Height.Set(400, 0); detailsList.Add(right);
        Place(title, right, 0, 0, 1, 0, 28);
        Place(detail, right, 0, 36, 1, 0, 156); detail.IsWrapped = true;
        Place(hint, right, 0, 194, 1, 0, 60); hint.IsWrapped = true;
        ActionButton(right, "Upgrade", 0, 0, () => Request(TalentOperation.Upgrade), () => CanAct && Player.State.AvailableTalentPoints > 0 && NumericTalents.TryGet(selected, out _));
        ActionButton(right, "RefundOne", 1, 0, () => Request(TalentOperation.RefundOne), () => CanAct && HasSelected);
        ActionButton(right, "Enable", 0, 1, () => Request(TalentOperation.Enable), () => CanAct && HasSelected && !Player.State.Talents[selected].Enabled);
        ActionButton(right, "Disable", 1, 1, () => Request(TalentOperation.Disable), () => CanAct && HasSelected && Player.State.Talents[selected].Enabled);
        ActionButton(right, "RefundTalent", 0, 2, () => Request(TalentOperation.RefundTalent), () => CanAct && HasSelected);
        ActionButton(right, "RefundCategory", 1, 2, () => Player.RequestTalent(TalentOperation.RefundCategory, category: category), () => CanAct && Player.State.Talents.Keys.Any(id => NumericTalents.TryGet(id, out var d) && d.Category == category));
        var footerOps = new[] { TalentOperation.EnableEverything, TalentOperation.DisableEverything, TalentOperation.RefundEverything };
        for (int i = 0; i < footerOps.Length; i++) {
            var op = footerOps[i];
            var button = Button(op.ToString(), () => Player.RequestTalent(op), () => CanAct && Player.State.Talents.Count > 0);
            button.Left.Set(i * 4, i / 3f); button.Top.Set(-66, 1); button.Width.Set(-8, 1 / 3f); panel.Append(button);
        }
        Place(feedback, panel, 0, -27, 1, -100, 25, 1);
        var resync = Button("Resync", () => ProgressionNetwork.RequestSnapshot(), () => Main.netMode == NetmodeID.MultiplayerClient && Player.RequestTimedOut);
        resync.Left.Set(-90, 1); resync.Top.Set(-30, 1); resync.Width.Set(90, 0); panel.Append(resync);
        SelectCategory(TalentCategory.BaseStats);
    }
    private void Request(TalentOperation operation) => Player.RequestTalent(operation, selected, category);
    private void SelectCategory(TalentCategory value)
    {
        category = value;
        list.Clear();
        var talents = NumericTalents.All.Where(t => t.Category == category).ToArray();
        selected = talents.FirstOrDefault()?.Id ?? "";
        foreach (var definition in talents) {
            var id = definition.Id;
            var entry = new TalentButton(() => {
                var owned = Player.State.Talents.GetValueOrDefault(id);
                return Name(id) + "  " + Compact(owned?.TalentLevel ?? 0);
            }, () => selected = id, () => true);
            entry.Width.Set(0, 1); entry.Height.Set(34, 0);
            list.Add(entry);
        }
    }
    private void ActionButton(UIElement parent, string key, int column, int row, Action action, Func<bool> enabled)
    {
        var button = Button(key, action, enabled);
        button.Left.Set(column * 4, column * .5f); button.Top.Set(-110 + row * 36, 1); button.Width.Set(-4, .5f); parent.Append(button);
    }
    private static TalentButton Button(string key, Action action, Func<bool>? enabled = null) => new(() => Text(key), action, enabled ?? (() => true));
    private static void Place(UIElement item, UIElement parent, float x, float y, float widthPercent, float widthPixels, float height, float topPercent = 0)
    {
        item.Left.Set(x, 0); item.Top.Set(y, topPercent); item.Width.Set(widthPixels, widthPercent); item.Height.Set(height, 0); parent.Append(item);
    }
    internal static string Compact(BigInteger value) => value.ToString().Length <= 10 ? value.ToString() : value.ToString()[..4] + "… (" + value.ToString().Length + Text("Digits") + ")";
    private static string Effect(NumericTalent definition, BigInteger level)
    {
        if (definition.Unit == EffectUnit.RemainingMultiplier)
            return Text("Remaining", (100 * TalentMath.Remaining((double)definition.PerLevel, level)).ToString("0.##", CultureInfo.InvariantCulture));
        // Defaults have at most two decimal places, preserving huge integer levels.
        var hundredths = level * (int)(definition.PerLevel * 100);
        string value = hundredths < 1000000000000 ? ((decimal)hundredths / 100).ToString("0.##", CultureInfo.InvariantCulture) : Compact(hundredths / 100);
        return "+" + value + (definition.Unit == EffectUnit.Percent ? "%" : definition.Unit == EffectUnit.PerSecond ? Text("PerSecond") : "");
    }
    public override void Update(GameTime gameTime)
    {
        float width = Math.Min(960, Main.screenWidth / Main.UIScale - 24);
        float height = Math.Min(700, Main.screenHeight / Main.UIScale - 24);
        if (width != lastWidth || height != lastHeight) {
            panel.Width.Set(width, 0); panel.Height.Set(height, 0);
            lastWidth = width; lastHeight = height; Recalculate();
        }
        if (Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape)) {
            TalentUISystem.Toggle();
            return;
        }
        if (panel.ContainsPoint(Main.MouseScreen)) {
            Main.LocalPlayer.mouseInterface = true;
            PlayerInput.LockVanillaMouseScroll("TerrariaProgression/Talents");
        }
        var s = Player.State;
        var cap = ModContent.GetInstance<ProgressionConfig>().ExperienceRequirementCap;
        header.SetText(Text("Header", Compact(s.Level), Experience.Format(s.CurrentExperience), Experience.Format(Experience.Requirement(s.Level, cap))));
        totals.SetText(Text("Totals", Compact(s.AvailableTalentPoints), Compact(s.TotalSpentTalentPoints), Experience.Format(s.TotalExperienceEarned)));
        if (NumericTalents.TryGet(selected, out var definition)) {
            var owned = s.Talents.GetValueOrDefault(selected);
            var level = owned?.TalentLevel ?? 0;
            title.SetText(Name(selected));
            detail.SetText(Text("Detail", Compact(level), owned?.Enabled == false ? Text("Off") : level > 0 ? Text("On") : Text("Unlearned"),
                Effect(definition, owned?.Enabled == false ? 0 : level), Effect(definition, level + 1), definition.DefaultCost, Compact(owned?.InvestedPoints ?? 0)));
            hint.SetText(Language.GetTextValue("Mods.TerrariaProgression.TalentHints." + selected));
        }
        else { title.SetText(Text("Category" + (int)category)); detail.SetText(Text("ComingLater")); hint.SetText(""); }
        feedback.SetText(!Player.SessionReady ? Text("Waiting") : Player.RequestTimedOut ? Text("Timeout") : Player.RequestPending ? Text("Waiting") : Player.HasResult ? Text("Result" + Player.LastResult) : Text("Help"));
        base.Update(gameTime);
    }
}

internal sealed class TalentButton : UITextPanel<string>
{
    private readonly Func<string> label;
    private readonly Func<bool> enabled;
    public TalentButton(Func<string> label, Action action, Func<bool> enabled) : base("", .72f)
    {
        this.label = label; this.enabled = enabled;
        Height.Set(32, 0); SetPadding(5);
        OnLeftClick += (_, _) => { if (enabled()) action(); };
    }
    public override void Update(GameTime gameTime)
    {
        var text = label();
        float textWidth = Terraria.GameContent.FontAssets.MouseText.Value.MeasureString(text).X;
        float scale = Math.Min(.72f, Math.Max(.25f, (GetInnerDimensions().Width - 6) / Math.Max(1, textWidth)));
        SetText(text, scale, false);
        bool active = enabled();
        TextColor = active ? Color.White : new Color(123, 135, 150);
        BackgroundColor = active && IsMouseHovering ? new Color(45, 93, 105) : new Color(30, 44, 65);
        BorderColor = active && IsMouseHovering ? new Color(214, 181, 105) : new Color(63, 80, 103);
        base.Update(gameTime);
    }
}
