using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameInput;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader.Config.UI;


namespace TerrariaProgression.Config;

// Re-read the pending values so Restore updates the visible controls immediately.
public sealed class TalentLimitInputElement : ConfigElement<string>
{
    private LimitTextInput? input;
    private string shown = "";
    public override void OnBind()
    {
        base.OnBind();
        var panel = new UIPanel(); panel.SetPadding(0); panel.Left.Set(-195,1); panel.Width.Set(185,0); panel.Height.Set(30,0);
        input = new LimitTextInput("-1"); input.Left.Set(8,0); input.Top.Set(5,0); input.Width.Set(-16,1); input.Height.Set(20,0);
        shown = Value; input.SetText(shown);
        input.Changed += () => { shown=input.CurrentString; Value=shown; };
        panel.Append(input); Append(panel);
    }
    public override void Update(GameTime gameTime)
    {
        if(input!=null&&shown!=Value) { shown=Value; input.SetText(shown); }
        base.Update(gameTime);
    }
}

public sealed class TalentOverridesElement : ConfigElement<List<TalentLevelOverride>>
{
    private List<TalentLevelOverride>? shown;
    private bool rebuild;
    public override void OnBind() { base.OnBind(); Build(); }
    private void Build()
    {
        RemoveAllChildren(); shown=Value; rebuild=false;
        var add=new UITextPanel<string>("+",.8f); add.Left.Set(-48,1); add.Width.Set(40,0); add.Height.Set(28,0);
        add.OnLeftClick+=(_,_)=>{ var entries=Value??new(); entries.Add(new()); Value=entries; rebuild=true; }; Append(add);
        for(int index=0;index<(shown?.Count??0);index++) {
            int row=index; var entry=shown![row];
            var panel=new UIPanel();panel.SetPadding(0);panel.Top.Set(34+row*36,0);panel.Width.Set(-8,1);panel.Height.Set(32,0);Append(panel);
            var id=new LimitTextInput("Talent ID");id.SetText(entry.TalentId);id.Left.Set(8,0);id.Top.Set(6,0);id.Width.Set(-12,.6f);id.Height.Set(20,0);
            var limit=new LimitTextInput("-1");limit.SetText(entry.Limit);limit.Left.Set(0,.6f);limit.Top.Set(6,0);limit.Width.Set(-48,.4f);limit.Height.Set(20,0);
            id.Changed+=()=>{entry.TalentId=id.CurrentString;SetObject(Value);};
            limit.Changed+=()=>{entry.Limit=limit.CurrentString;SetObject(Value);};
            var remove=new UITextPanel<string>("×",.8f);remove.Left.Set(-36,1);remove.Width.Set(32,0);remove.Height.Set(28,0);
            remove.OnLeftClick+=(_,_)=>{Value.RemoveAt(row);SetObject(Value);rebuild=true;};
            panel.Append(id);panel.Append(limit);panel.Append(remove);
        }
        Height.Set(34+(shown?.Count??0)*36,0);
        if(Parent!=null) { Parent.Height.Set(Height.Pixels,0); Parent.Recalculate(); }
    }
    public override void Update(GameTime gameTime)
    {
        if(rebuild||!ReferenceEquals(shown,Value)) Build();
        base.Update(gameTime);
    }
}

internal sealed class LimitTextInput : UITextPanel<string>
{
    private static LimitTextInput? focused;
    public string CurrentString { get; private set; } = "";
    public event Action? Changed;
    public LimitTextInput(string placeholder) : base(placeholder,.75f)
    {
        SetPadding(0);TextHAlign=0;
        OnLeftClick+=(_,_)=>{focused=this;Main.clrInput();};
    }
    public new void SetText(string text) { CurrentString=text??"";base.SetText(CurrentString); }
    public override void Update(GameTime gameTime)
    {
        if(focused==this) {
            if(Main.mouseLeft&&Main.mouseLeftRelease&&!ContainsPoint(Main.MouseScreen)) focused=null;
            else {
                PlayerInput.WritingText=true; Main.blockInput=true;
                Main.instance.HandleIME();
                string next=Main.GetInputText(CurrentString);
                if(next.Length>2048)next=next[..2048];
                if(next!=CurrentString){SetText(next);Changed?.Invoke();}
                if(Main.keyState.IsKeyDown(Keys.Enter)||Main.keyState.IsKeyDown(Keys.Escape)) {focused=null;Main.blockInput=false;PlayerInput.WritingText=false;}
            }
        }
        BorderColor=focused==this?Color.Goldenrod:Color.Transparent;
        base.Update(gameTime);
    }
}
