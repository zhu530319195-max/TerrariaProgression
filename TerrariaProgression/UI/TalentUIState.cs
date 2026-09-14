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
    private readonly UIList navigation = new(), list = new(), detailsList = new(), actionList = new();
    private readonly UIElement navArea = new(), left = new(), rightArea = new(), detailCard = new(), actions = new(), actionCard = new();
    private readonly List<TalentButton> actionButtons = new();
    private readonly UIText header = new("", .8f), totals = new("", .72f), title = new("", 1.05f);
    private readonly UIText detail = new("", .8f), hint = new("", .72f), feedback = new("", .7f), pathLabel = new("", .7f);
    private const TalentCategory category = TalentCategory.BaseStats; // legacy single-talent packet field; not a display category
    private string menuCategory = "Survival", menuGroup = "", selected = "MaxLife", query = "";
    private bool ownedOnly, enabledOnly;
    private ProgressionState? filteredState;
    private string filteredLanguage = "";
    private float lastWidth, lastHeight;
    private TalentButton? resync, search, ownedFilter, enabledFilter, clearSearch;
    internal bool SearchFocused { get; private set; }
    private readonly List<TalentButton> childButtons = new();
    private string childSelection = "";
    private ProgressionPlayer Player => Main.LocalPlayer.GetModPlayer<ProgressionPlayer>();
    internal static string Text(string key, params object[] args) => Language.GetTextValue("Mods.TerrariaProgression.TalentsUI." + key, args);
    private static string Name(string id) => Language.GetTextValue("Mods.TerrariaProgression.TalentNames." + id);
    private bool CanAct => Player.SessionReady && !Player.RequestPending && !Player.RequestCoolingDown && !Main.LocalPlayer.dead;
    private bool HasSelected => Player.State.Talents.ContainsKey(selected);
    public override void OnInitialize()
    {
        panel.BackgroundColor = new Color(20, 29, 48, 248);
        panel.BorderColor = new Color(176, 146, 84); panel.SetPadding(12);
        foreach (var label in new[] { header, totals, title, detail, hint, feedback, pathLabel }) {
            label.TextOriginX = 0; label.WrappedTextBottomPadding = 0; label.DynamicallyScaleDownToWidth = true;
        }
        title.TextColor = new Color(229,199,132); totals.TextColor = new Color(160,213,208);
        hint.TextColor = pathLabel.TextColor = new Color(174,188,209);
        panel.HAlign = panel.VAlign = .5f; Append(panel);
        Place(header,panel,0,3,1,-90,26); Place(totals,panel,0,31,1,0,22);
        var close=Button("Close",TalentUISystem.Toggle); close.Left.Set(-80,1); close.Width.Set(80,0); panel.Append(close);
        search=new TalentButton(() => Text("Search")+": "+query+(SearchFocused ? " |" : ""), () => { SearchFocused=true; Main.clrInput(); Main.blockInput=true; }, () => true, () => SearchFocused);
        search.TextHAlign=0; panel.Append(search);
        clearSearch=Button("ClearSearch",()=>{query=""; EndSearch(); RefreshTalents();}); panel.Append(clearSearch);
        ownedFilter=new TalentButton(()=>Text("OwnedOnly")+" "+Text(ownedOnly?"On":"Off"),()=>{ownedOnly=!ownedOnly;RefreshTalents();},()=>true,()=>ownedOnly); panel.Append(ownedFilter);
        enabledFilter=new TalentButton(()=>Text("EnabledOnly")+" "+Text(enabledOnly?"On":"Off"),()=>{enabledOnly=!enabledOnly;RefreshTalents();},()=>true,()=>enabledOnly); panel.Append(enabledFilter);
        Place(pathLabel,panel,0,100,1,0,22);
        panel.Append(navArea); panel.Append(left); panel.Append(rightArea);
        AttachList(navArea,navigation); AttachList(left,list); AttachList(rightArea,detailsList);
        detailCard.Width.Set(0,1); detailsList.Add(detailCard);
        Place(title,detailCard,0,0,1,0,26); Place(detail,detailCard,0,32,1,0,0); detail.IsWrapped=true;
        Place(hint,detailCard,0,140,1,0,0); hint.IsWrapped=true;
        actions.Width.Set(0,1); rightArea.Append(actions); AttachList(actions,actionList);
        actionCard.Width.Set(0,1); actionList.Add(actionCard);
        AddAction(()=>TalentCatalog.TryGet(selected,out var d)?Text(d.MaxLevel==1?"UnlockCost":"UpgradeCost",d.DefaultCost):Text("Upgrade"),
            ()=>Request(TalentOperation.Upgrade),()=>CanAct && TalentCatalog.TryGet(selected,out var d) && Player.State.AvailableTalentPoints>=d.DefaultCost && (d.MaxLevel==0 || (Player.State.Talents.GetValueOrDefault(selected)?.TalentLevel??0)<d.MaxLevel));
        AddAction(()=>Text("RefundOneAmount",Compact(HasSelected?Player.State.Talents[selected].CostRuns[^1].Cost:0)),()=>Request(TalentOperation.RefundOne),()=>CanAct&&HasSelected);
        AddAction(()=>Text("RefundTalentAmount",Compact(HasSelected?Player.State.Talents[selected].InvestedPoints:0)),()=>Request(TalentOperation.RefundTalent),()=>CanAct&&HasSelected);
        AddAction(()=>Text("Enable"),()=>Request(TalentOperation.Enable),()=>CanAct&&HasSelected&&!Player.State.Talents[selected].Enabled);
        AddAction(()=>Text("Disable"),()=>Request(TalentOperation.Disable),()=>CanAct&&HasSelected&&Player.State.Talents[selected].Enabled);
        foreach(var op in new[]{TalentOperation.DecreaseIntensity,TalentOperation.IncreaseIntensity,TalentOperation.MaximumIntensity})
            AddAction(()=>Text(op.ToString()),()=>Request(op),()=>CanAct&&HasSelected&&TalentCatalog.TryGet(selected,out var d)&&d.Adjustable);
        AddAction(()=>Text("RefundGroupAmount",ScopeLabel(true),Compact(TalentNavigation.RefundAmount(Player.State,RefundScope(true),true))),
            ()=>Player.RequestTalent(TalentOperation.RefundMenuGroup,RefundScope(true)),()=>CanAct&&TalentNavigation.RefundAmount(Player.State,RefundScope(true),true)>0);
        AddAction(()=>Text("RefundMenuAmount",ScopeLabel(false),Compact(TalentNavigation.RefundAmount(Player.State,RefundScope(false),false))),
            ()=>Player.RequestTalent(TalentOperation.RefundMenuCategory,RefundScope(false)),()=>CanAct&&TalentNavigation.RefundAmount(Player.State,RefundScope(false),false)>0);
        var footerOps=new[]{TalentOperation.EnableEverything,TalentOperation.DisableEverything,TalentOperation.RefundEverything};
        for(int i=0;i<footerOps.Length;i++) {
            var op=footerOps[i];
            var button=new TalentButton(()=>op==TalentOperation.RefundEverything?Text("RefundAllAmount",Compact(Player.State.TotalSpentTalentPoints)):Text(op.ToString()),()=>Player.RequestTalent(op),()=>CanAct&&Player.State.Talents.Count>0);
            button.Left.Set(i*4,i/3f); button.Top.Set(-62,1); button.Width.Set(-8,1/3f); panel.Append(button);
        }
        Place(feedback,panel,0,-27,1,0,25,1);
        resync=Button("Resync",()=>ProgressionNetwork.RequestSnapshot(),()=>Main.netMode==NetmodeID.MultiplayerClient&&Player.RequestTimedOut);
        resync.Left.Set(-90,1); resync.Top.Set(-30,1); resync.Width.Set(90,0);
        // Initialization builds only the layout. The first in-world Update
        // binds the current character, including after changing characters.
        RefreshNavigation();
    }
    private static void AttachList(UIElement parent,UIList target)
    {
        target.Width.Set(-20,1); target.Height.Set(0,1); target.ListPadding=5; target.ManualSortMethod=_=>{}; parent.Append(target);
        var scroll=new TalentScrollbar(target); scroll.Left.Set(-18,1); scroll.Height.Set(0,1); parent.Append(scroll); target.SetScrollbar(scroll);
    }
    internal void EndSearch() { if(SearchFocused) {SearchFocused=false; Main.blockInput=false; PlayerInput.WritingText=false;} }
    public override void OnDeactivate() { EndSearch(); base.OnDeactivate(); }
    private string ScopeLabel(bool group) => RefundScope(group).Length == 0 ? Text("NoSelection") : Text((group ? "MenuGroup" : "Menu") + RefundScope(group));
    private string RefundScope(bool group)
    {
        var location=TalentNavigation.Find(selected);
        // Search is global: scope labels and requests follow the selected result.
        if(query.Trim().Length>0) return location==null?"":group?location.Id:location.CategoryId;
        return group?(menuGroup.Length>0?menuGroup:location?.Id??""):menuCategory;
    }
    private void Request(TalentOperation operation) => Player.RequestTalent(operation,selected,category);
    private void Choose(string cat,string group)
    {
        menuCategory=cat; menuGroup=group; query=""; EndSearch(); RefreshNavigation(); RefreshTalents();
    }
    private void RefreshNavigation()
    {
        float scroll=navigation.ViewPosition; navigation.Clear();
        foreach(var cat in TalentNavigation.Categories) {
            var groups=TalentNavigation.Groups.Where(g=>g.CategoryId==cat&&g.TalentIds.Any(id=>TalentCatalog.TryGet(id,out _))).ToArray();
            if(groups.Length==0)continue;
            var button=new TalentButton(()=> (menuCategory==cat?"− ":"+ ")+Text("Menu"+cat),()=>Choose(cat,""),()=>true,()=>menuCategory==cat);
            button.Width.Set(0,1); button.TextHAlign=0; navigation.Add(button);
            if(menuCategory!=cat)continue;
            var all=new TalentButton(()=>"  "+Text("AllInCategory"),()=>Choose(cat,""),()=>true,()=>menuGroup.Length==0&&query.Length==0);
            all.Width.Set(0,1); all.TextHAlign=0; navigation.Add(all);
            foreach(var g in groups) {
                var entry=new TalentButton(()=>"  "+Text("MenuGroup"+g.Id),()=>Choose(cat,g.Id),()=>true,()=>menuGroup==g.Id&&query.Length==0);
                entry.Width.Set(0,1); entry.TextHAlign=0; navigation.Add(entry);
            }
        }
        navigation.ViewPosition=scroll;
    }
    private void RefreshTalents()
    {
        float scroll=list.ViewPosition; list.Clear();
        var ids=TalentNavigation.Visible(Player.State,menuCategory,menuGroup,query,ownedOnly,enabledOnly,Name);
        if(!ids.Contains(selected)) {selected=ids.FirstOrDefault()??"";scroll=0;detailsList.ViewPosition=0;}
        foreach(var id in ids) {
            TalentCatalog.TryGet(id,out var definition);
            var entry=new TalentButton(()=> {
                var owned=Player.State.Talents.GetValueOrDefault(id);
                return Name(id)+"  "+(definition.MaxLevel==1?Text(owned==null?"Locked":"Unlocked"):Compact(owned?.TalentLevel??0));
            },()=>{selected=id;detailsList.ViewPosition=0;},()=>true,()=>selected==id);
            entry.Width.Set(0,1); entry.TextHAlign=0; entry.PaddingLeft=8; list.Add(entry);
            if(query.Trim().Length>0 && TalentNavigation.Find(id) is {} g) {
                var location=new UIText(Text("Menu"+g.CategoryId)+" / "+Text("MenuGroup"+g.Id),.6f) {TextColor=new Color(174,188,209),DynamicallyScaleDownToWidth=true};
                location.Width.Set(0,1);location.Height.Set(20,0);location.MinWidth.Set(0,0);list.Add(location);
            }
        }
        list.ViewPosition=scroll; filteredState=Player.State; filteredLanguage=Language.ActiveCulture.Name;
    }
    private void AddAction(Func<string> text,Action action,Func<bool> enabled)
    {var b=new TalentButton(text,action,enabled);actionButtons.Add(b);actionCard.Append(b);}
    private static TalentButton Button(string key,Action action,Func<bool>? enabled=null)=>new(()=>Text(key),action,enabled??(()=>true));
    private static void Place(UIElement item,UIElement parent,float x,float y,float wp,float w,float h,float tp=0)
    {item.Left.Set(x,0);item.Top.Set(y,tp);item.Width.Set(w,wp);item.Height.Set(h,0);parent.Append(item);}
    internal static string Compact(BigInteger value)=>value.ToString().Length<=10?value.ToString():value.ToString()[..4]+"… ("+value.ToString().Length+Text("Digits")+")";
    private static string Xp(BigInteger units) {string value=Experience.Format(units);return value.Length<=12?value:Compact(units/Experience.Scale);}
    private static string Effect(TalentDefinition definition,BigInteger level)
    {
        if(definition.Unit==EffectUnit.Flag)return Text(level>0?"On":"Off");
        if(definition.Unit==EffectUnit.RemainingMultiplier)return Text(definition.Id=="CrateChance"?"CrateRemaining":definition.Id=="BaitSaving"?"BaitRemaining":"Remaining",(100*TalentMath.Remaining((double)definition.PerLevel,level)).ToString("0.##",CultureInfo.InvariantCulture));
        var hundredths=level*(int)(definition.PerLevel*100);
        string value=hundredths<1000000000000?((decimal)hundredths/100).ToString("0.##",CultureInfo.InvariantCulture):Compact(hundredths/100);
        if(definition.Unit==EffectUnit.Multiplier)return Text("NativeDamageMultiplier",value);
        if(definition.Unit==EffectUnit.Seconds)return Text("EffectSeconds",value);
        return "+"+value+(definition.Unit==EffectUnit.Percent?"%":definition.Unit==EffectUnit.PerSecond?Text("PerSecond"):"");
    }
    private void Resize(float width,float height)
    {
        panel.Width.Set(width,0);panel.Height.Set(height,0);
        search!.Left.Set(0,0);search.Top.Set(62,0);search.Width.Set(-8,.44f);
        clearSearch!.Left.Set(0,.44f);clearSearch.Top.Set(62,0);clearSearch.Width.Set(-6,.12f);
        ownedFilter!.Left.Set(0,.56f);ownedFilter.Top.Set(62,0);ownedFilter.Width.Set(-6,.22f);
        enabledFilter!.Left.Set(0,.78f);enabledFilter.Top.Set(62,0);enabledFilter.Width.Set(0,.22f);
        const float contentTop=128;
        navArea.Top.Set(contentTop,0);navArea.Width.Set(-8,.24f);navArea.Height.Set(-contentTop-72,1);
        left.Left.Set(0,.24f);left.Top.Set(contentTop,0);left.Width.Set(-8,.30f);left.Height.Set(-contentTop-72,1);
        rightArea.Left.Set(0,.54f);rightArea.Top.Set(contentTop,0);rightArea.Width.Set(0,.46f);rightArea.Height.Set(-contentTop-72,1);
        float actionHeight=Math.Min(150,Math.Max(72,(height-24-contentTop-72)*.45f));
        detailsList.Height.Set(-actionHeight-8,1);
        foreach(var child in rightArea.Children)if(child is TalentScrollbar)child.Height.Set(-actionHeight-8,1);
        actions.Height.Set(actionHeight,0);actions.Top.Set(-actionHeight,1);
        // Two columns; long scoped refunds occupy an entire row. Scroll when height is limited.
        for(int i=0;i<actionButtons.Count;i++) {
            bool wide=i>=8;
            actionButtons[i].Left.Set(0,wide?0:i%2/2f);
            actionButtons[i].Top.Set((wide?4+i-8:i/2)*36,0);
            actionButtons[i].Width.Set(wide?0:-5,wide?1:.5f);
        }
        actionCard.Height.Set(6*36,0);Recalculate();Recalculate();
    }
    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        if(SearchFocused && search!=null) Main.instance.DrawWindowsIMEPanel(search.GetDimensions().Position()+new Microsoft.Xna.Framework.Vector2(0,32),1);
    }
    private void FitDetailText()
    {
        detail.Recalculate();
        float hintTop = detail.Top.Pixels + detail.MinHeight.Pixels + 12;
        hint.Top.Set(hintTop, 0); hint.Recalculate();
        float height = hintTop + hint.MinHeight.Pixels + 8;
        if (childSelection != selected) {
            foreach (var button in childButtons) button.Remove();
            childButtons.Clear(); childSelection = selected;
            if (FunctionalTalentRegistry.TryGet(selected, out var functional))
                for (int i = 0; i < functional.ChildEffects.Count; i++) {
                    int index = i + 1; string child = functional.ChildEffects[i]; string parent = selected;
                    var button = new TalentButton(() => Text("Child" + child) + " · " + Text(FunctionalTalentRegistry.ChildEnabled(Player.State,parent,child) ? "On" : "Off"),
                        () => Player.RequestTalent(TalentOperation.ToggleChild,parent,category,index), () => CanAct && Player.State.Talents.ContainsKey(parent));
                    button.Width.Set(0,1); childButtons.Add(button); detailCard.Append(button);
                }
        }
        for (int i = 0; i < childButtons.Count; i++) { childButtons[i].Top.Set(height + i * 36,0); childButtons[i].Recalculate(); }
        height += childButtons.Count * 36;
        if (Math.Abs(detailCard.Height.Pixels - height) > .1f) {
            detailCard.Height.Set(height, 0);
            detailsList.Recalculate();
        }
    }
    public override void Update(GameTime gameTime)
    {
        float width = Math.Min(1180, Main.screenWidth / Main.UIScale - 24);
        float height = Math.Min(800, Main.screenHeight / Main.UIScale - 24);
        if (width != lastWidth || height != lastHeight) {
            lastWidth = width; lastHeight = height; Resize(width, height);
        }
        if (SearchFocused) {
            if (Main.mouseLeft && Main.mouseLeftRelease && search!=null && !search.ContainsPoint(Main.MouseScreen)) EndSearch();
            else {
                Main.blockInput=true; PlayerInput.WritingText=true; Main.instance.HandleIME();
                string input=Main.GetInputText(query); if(input.Length>80)input=input[..80];
                if(input!=query){query=input;RefreshTalents();}
                if(Main.keyState.IsKeyDown(Keys.Enter)||Main.keyState.IsKeyDown(Keys.Escape)){EndSearch();base.Update(gameTime);return;}
            }
        }
        if (!ReferenceEquals(filteredState,Player.State) || filteredLanguage!=Language.ActiveCulture.Name) RefreshTalents();
        var location=TalentNavigation.Find(selected);
        pathLabel.SetText(query.Trim().Length>0?Text("GlobalResults",query):Text("Menu"+menuCategory)+" / "+(menuGroup.Length>0?Text("MenuGroup"+menuGroup):Text("AllInCategory")));
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
        if (TalentCatalog.TryGet(selected, out var definition)) {
            var owned = s.Talents.GetValueOrDefault(selected);
            var level = owned?.TalentLevel ?? 0;
            title.SetText(Name(selected));
            if (definition.MaxLevel == 1) detail.SetText(Text("UnlockDetail", Text(level > 0 ? "Unlocked" : "Locked"),
                Text(owned?.Enabled == true ? "On" : "Off"), definition.DefaultCost, Compact(owned?.InvestedPoints ?? 0)));
            else detail.SetText(Text("Detail", Compact(level), owned?.Enabled == false ? Text("Off") : level > 0 ? Text("On") : Text("Unlearned"),
                Effect(definition, NumericTalents.ActiveLevel(s, selected)), Effect(definition, level + 1), definition.DefaultCost, Compact(owned?.InvestedPoints ?? 0)));
            hint.SetText(Language.GetTextValue("Mods.TerrariaProgression.TalentHints." + selected) +
                (definition.Adjustable ? "\n" + Text("Intensity", Compact(owned == null ? 0 : NumericTalents.EffectiveLevel(owned))) : ""));
        }
        else { title.SetText(Text("NoResults")); detail.SetText(Text("NoResultsHint")); hint.SetText(""); }
        feedback.SetText(!Player.SessionReady ? Text("Waiting") : Player.RequestTimedOut ? Text("Timeout") : Player.RequestPending ? Text("Waiting") : Player.HasResult ? Text("Result" + Player.LastResult) : Text("Help"));
        // Non-wrapped UIText updates its minimum width when text changes; release
        // that minimum so long numbers can fit instead of expanding over the close button.
        foreach (var label in new[] { header, totals, title, feedback, pathLabel }) label.MinWidth.Set(0, 0);
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
