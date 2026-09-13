using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
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
    private readonly UIList list = new(), detailsList = new();
    private readonly UIElement left = new(), rightArea = new(), detailCard = new(), actions = new();
    private readonly List<TalentButton> tabs = new();
    private readonly List<TalentButton> actionButtons = new();
    private readonly UIText header = new("", .8f), totals = new("", .72f), title = new("", 1.05f);
    private readonly UIText detail = new("", .8f), hint = new("", .72f), feedback = new("", .7f);
    private TalentCategory category;
    private string selected = "MaxLife";
    private float lastWidth, lastHeight;
    private TalentButton? resync;
    private ProgressionPlayer Player => Main.LocalPlayer.GetModPlayer<ProgressionPlayer>();
    internal static string Text(string key, params object[] args) => Language.GetTextValue("Mods.TerrariaProgression.TalentsUI." + key, args);
    private static string Name(string id) => Language.GetTextValue("Mods.TerrariaProgression.TalentNames." + id);
    private bool CanAct => Player.SessionReady && !Player.RequestPending && !Player.RequestCoolingDown && !Main.LocalPlayer.dead;
    private bool HasSelected => Player.State.Talents.ContainsKey(selected);
    public override void OnInitialize()
    {
        panel.BackgroundColor = new Color(20, 29, 48, 248);
        panel.BorderColor = new Color(176, 146, 84);
        panel.SetPadding(12);
        foreach (var label in new[] { header, totals, title, detail, hint, feedback }) {
            label.TextOriginX = 0;
            label.WrappedTextBottomPadding = 0;
            label.DynamicallyScaleDownToWidth = true;
        }
        title.TextColor = new Color(229, 199, 132);
        totals.TextColor = new Color(160, 213, 208);
        hint.TextColor = new Color(174, 188, 209);
        panel.HAlign = panel.VAlign = .5f;
        Append(panel);
        Place(header, panel, 0, 3, 1, -100, 26);
        Place(totals, panel, 0, 31, 1, 0, 22);
        var close = Button("Close", () => TalentUISystem.Toggle());
        close.Left.Set(-80, 1); close.Top.Set(0, 0); close.Width.Set(80, 0); panel.Append(close);
        for (int i = 0; i < 6; i++) {
            var value = (TalentCategory)i;
            var tab = new TalentButton(() => Text("Category" + (int)value), () => SelectCategory(value), () => true, () => category == value);
            tabs.Add(tab);
            panel.Append(tab);
        }
        panel.Append(left);
        list.Width.Set(-20, 1); list.Height.Set(0, 1); list.ListPadding = 5;
        list.ManualSortMethod = _ => { }; left.Append(list);
        var scroll = new TalentScrollbar(list);
        scroll.Left.Set(-18, 1); scroll.Height.Set(0, 1); left.Append(scroll);
        list.SetScrollbar(scroll);
        panel.Append(rightArea);
        detailsList.Width.Set(-20, 1); detailsList.Height.Set(-114, 1); rightArea.Append(detailsList);
        var detailScroll = new TalentScrollbar(detailsList);
        detailScroll.Left.Set(-18, 1); detailScroll.Height.Set(-114, 1); rightArea.Append(detailScroll);
        detailsList.SetScrollbar(detailScroll);
        detailCard.Width.Set(0, 1); detailsList.Add(detailCard);
        Place(title, detailCard, 0, 0, 1, 0, 26);
        Place(detail, detailCard, 0, 32, 1, 0, 0); detail.IsWrapped = true;
        Place(hint, detailCard, 0, 140, 1, 0, 0); hint.IsWrapped = true;
        actions.Width.Set(0, 1); actions.Height.Set(106, 0); actions.Top.Set(-106, 1); rightArea.Append(actions);
        ActionButton(actions, "Upgrade", () => Request(TalentOperation.Upgrade), () => CanAct && Player.State.AvailableTalentPoints > 0 && NumericTalents.TryGet(selected, out _));
        ActionButton(actions, "RefundOne", () => Request(TalentOperation.RefundOne), () => CanAct && HasSelected);
        ActionButton(actions, "RefundTalent", () => Request(TalentOperation.RefundTalent), () => CanAct && HasSelected);
        ActionButton(actions, "Enable", () => Request(TalentOperation.Enable), () => CanAct && HasSelected && !Player.State.Talents[selected].Enabled);
        ActionButton(actions, "Disable", () => Request(TalentOperation.Disable), () => CanAct && HasSelected && Player.State.Talents[selected].Enabled);
        ActionButton(actions, "RefundCategory", () => Player.RequestTalent(TalentOperation.RefundCategory, category: category), () => CanAct && Player.State.Talents.Keys.Any(id => NumericTalents.TryGet(id, out var d) && d.Category == category));
        foreach (var op in new[] { TalentOperation.DecreaseIntensity, TalentOperation.IncreaseIntensity, TalentOperation.MaximumIntensity })
            ActionButton(actions, op.ToString(), () => Request(op), () => CanAct && HasSelected && NumericTalents.TryGet(selected, out var d) && d.Adjustable);
        var footerOps = new[] { TalentOperation.EnableEverything, TalentOperation.DisableEverything, TalentOperation.RefundEverything };
        for (int i = 0; i < footerOps.Length; i++) {
            var op = footerOps[i];
            var button = Button(op.ToString(), () => Player.RequestTalent(op), () => CanAct && Player.State.Talents.Count > 0);
            button.Left.Set(i * 4, i / 3f); button.Top.Set(-62, 1); button.Width.Set(-8, 1 / 3f); panel.Append(button);
        }
        Place(feedback, panel, 0, -27, 1, 0, 25, 1);
        resync = Button("Resync", () => ProgressionNetwork.RequestSnapshot(), () => Main.netMode == NetmodeID.MultiplayerClient && Player.RequestTimedOut);
        resync.Left.Set(-90, 1); resync.Top.Set(-30, 1); resync.Width.Set(90, 0);
        SelectCategory(TalentCategory.BaseStats);
    }
    private void Request(TalentOperation operation) => Player.RequestTalent(operation, selected, category);
    private void SelectCategory(TalentCategory value)
    {
        category = value;
        list.Clear();
        list.ViewPosition = 0;
        detailsList.ViewPosition = 0;
        var talents = NumericTalents.All.Where(t => t.Category == category).ToArray();
        selected = talents.FirstOrDefault()?.Id ?? "";
        foreach (var definition in talents) {
            var id = definition.Id;
            var entry = new TalentButton(() => {
                var owned = Player.State.Talents.GetValueOrDefault(id);
                return Name(id) + "  " + Compact(owned?.TalentLevel ?? 0);
            }, () => { selected = id; detailsList.ViewPosition = 0; }, () => true, () => selected == id);
            entry.Width.Set(0, 1); entry.Height.Set(32, 0);
            entry.TextHAlign = 0; entry.PaddingLeft = 10;
            list.Add(entry);
        }
    }
    private void ActionButton(UIElement parent, string key, Action action, Func<bool> enabled)
    {
        var button = Button(key, action, enabled);
        actionButtons.Add(button);
        parent.Append(button);
    }
    private static TalentButton Button(string key, Action action, Func<bool>? enabled = null) => new(() => Text(key), action, enabled ?? (() => true));
    private static void Place(UIElement item, UIElement parent, float x, float y, float widthPercent, float widthPixels, float height, float topPercent = 0)
    {
        item.Left.Set(x, 0); item.Top.Set(y, topPercent); item.Width.Set(widthPixels, widthPercent); item.Height.Set(height, 0); parent.Append(item);
    }
    internal static string Compact(BigInteger value) => value.ToString().Length <= 10 ? value.ToString() : value.ToString()[..4] + "… (" + value.ToString().Length + Text("Digits") + ")";
    private static string Xp(BigInteger units)
    {
        string value = Experience.Format(units);
        return value.Length <= 12 ? value : Compact(units / Experience.Scale);
    }
    private static string Effect(NumericTalent definition, BigInteger level)
    {
        if (definition.Unit == EffectUnit.RemainingMultiplier)
            return Text("Remaining", (100 * TalentMath.Remaining((double)definition.PerLevel, level)).ToString("0.##", CultureInfo.InvariantCulture));
        // Defaults have at most two decimal places, preserving huge integer levels.
        var hundredths = level * (int)(definition.PerLevel * 100);
        string value = hundredths < 1000000000000 ? ((decimal)hundredths / 100).ToString("0.##", CultureInfo.InvariantCulture) : Compact(hundredths / 100);
        return "+" + value + (definition.Unit == EffectUnit.Percent ? "%" : definition.Unit == EffectUnit.PerSecond ? Text("PerSecond") : "");
    }
    private void Resize(float width, float height)
    {
        panel.Width.Set(width, 0); panel.Height.Set(height, 0);
        int columns = width >= 600 ? 6 : 3;
        int rows = 6 / columns;
        for (int i = 0; i < tabs.Count; i++) {
            tabs[i].Left.Set(0, (i % columns) / (float)columns);
            tabs[i].Top.Set(62 + i / columns * 34, 0);
            tabs[i].Width.Set(-5, 1f / columns);
            tabs[i].Height.Set(28, 0);
        }
        float contentTop = 62 + rows * 34 + 10;
        left.Top.Set(contentTop, 0); left.Width.Set(-12, .30f); left.Height.Set(-contentTop - 72, 1);
        rightArea.Left.Set(0, .30f); rightArea.Top.Set(contentTop, 0);
        rightArea.Width.Set(0, .70f); rightArea.Height.Set(-contentTop - 72, 1);
        for (int i = 0; i < actionButtons.Count; i++) {
            actionButtons[i].Left.Set(0, i % 3 / 3f);
            actionButtons[i].Top.Set(i / 3 * 36, 0);
            actionButtons[i].Width.Set(-5, 1f / 3);
        }
        Recalculate();
        // UIText wraps using its previous inner width. The second pass uses the new
        // viewport width so changing UI scale while open doesn't leave stale wrapping.
        Recalculate();
    }
    private void FitDetailText()
    {
        detail.Recalculate();
        float hintTop = detail.Top.Pixels + detail.MinHeight.Pixels + 12;
        hint.Top.Set(hintTop, 0); hint.Recalculate();
        float height = hintTop + hint.MinHeight.Pixels + 8;
        if (Math.Abs(detailCard.Height.Pixels - height) > .1f) {
            detailCard.Height.Set(height, 0);
            detailsList.Recalculate();
        }
    }
    public override void Update(GameTime gameTime)
    {
        float width = Math.Min(960, Main.screenWidth / Main.UIScale - 24);
        float height = Math.Min(700, Main.screenHeight / Main.UIScale - 24);
        if (width != lastWidth || height != lastHeight) {
            lastWidth = width; lastHeight = height; Resize(width, height);
        }
        if (Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape)) {
            TalentUISystem.Toggle();
            return;
        }
        if (panel.ContainsPoint(Main.MouseScreen)) {
            Main.LocalPlayer.mouseInterface = true;
            PlayerInput.LockVanillaMouseScroll("TerrariaProgression/Talents");
        }
        bool showResync = Main.netMode == NetmodeID.MultiplayerClient && Player.RequestTimedOut;
        if (resync != null && showResync != (resync.Parent != null)) {
            if (showResync) panel.Append(resync); else resync.Remove();
            feedback.Width.Set(showResync ? -100 : 0, 1);
            feedback.Recalculate();
        }
        var s = Player.State;
        var cap = ModContent.GetInstance<ProgressionConfig>().ExperienceRequirementCap;
        header.SetText(Text("Header", Compact(s.Level), Xp(s.CurrentExperience), Xp(Experience.Requirement(s.Level, cap))));
        totals.SetText(Text("Totals", Compact(s.AvailableTalentPoints), Compact(s.TotalSpentTalentPoints), Xp(s.TotalExperienceEarned)));
        if (NumericTalents.TryGet(selected, out var definition)) {
            var owned = s.Talents.GetValueOrDefault(selected);
            var level = owned?.TalentLevel ?? 0;
            title.SetText(Name(selected));
            detail.SetText(Text("Detail", Compact(level), owned?.Enabled == false ? Text("Off") : level > 0 ? Text("On") : Text("Unlearned"),
                Effect(definition, NumericTalents.ActiveLevel(s, selected)), Effect(definition, level + 1), definition.DefaultCost, Compact(owned?.InvestedPoints ?? 0)));
            hint.SetText(Language.GetTextValue("Mods.TerrariaProgression.TalentHints." + selected) +
                (definition.Adjustable ? "\n" + Text("Intensity", Compact(owned == null ? 0 : NumericTalents.EffectiveLevel(owned))) : ""));
        }
        else { title.SetText(Text("Category" + (int)category)); detail.SetText(Text("ComingLater")); hint.SetText(""); }
        feedback.SetText(!Player.SessionReady ? Text("Waiting") : Player.RequestTimedOut ? Text("Timeout") : Player.RequestPending ? Text("Waiting") : Player.HasResult ? Text("Result" + Player.LastResult) : Text("Help"));
        // Non-wrapped UIText updates its minimum width when text changes; release
        // that minimum so long numbers can fit instead of expanding over the close button.
        foreach (var label in new[] { header, totals, title, feedback }) label.MinWidth.Set(0, 0);
        FitDetailText();
        base.Update(gameTime);
    }
}

internal sealed class TalentButton : UITextPanel<string>
{
    private readonly Func<string> label;
    private readonly Func<bool> enabled;
    private readonly Func<bool> selected;
    public TalentButton(Func<string> label, Action action, Func<bool> enabled, Func<bool>? selected = null) : base("", .72f)
    {
        this.label = label; this.enabled = enabled; this.selected = selected ?? (() => false);
        Height.Set(32, 0); SetPadding(5);
        OnLeftClick += (_, _) => { if (enabled()) action(); };
    }
    public override void Update(GameTime gameTime)
    {
        var text = label();
        float textWidth = Terraria.GameContent.FontAssets.MouseText.Value.MeasureString(text).X;
        float scale = Math.Min(.72f, Math.Max(.25f, (GetInnerDimensions().Width - 6) / Math.Max(1, textWidth)));
        SetText(text, scale, false);
        MinWidth.Set(0, 0);
        bool active = enabled();
        bool highlighted = selected();
        TextColor = highlighted ? new Color(245, 215, 148) : active ? Color.White : new Color(123, 135, 150);
        BackgroundColor = highlighted ? new Color(40, 66, 79) : active && IsMouseHovering ? new Color(45, 93, 105) : new Color(30, 44, 65);
        BorderColor = highlighted || (active && IsMouseHovering) ? new Color(214, 181, 105) : new Color(63, 80, 103);
        base.Update(gameTime);
    }
}

// A full-length scrollbar adds noise when a category already fits in the viewport.
internal sealed class TalentScrollbar(UIList list) : UIScrollbar
{
    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (list.GetTotalHeight() > list.GetInnerDimensions().Height + 1) base.DrawSelf(spriteBatch);
    }
}
