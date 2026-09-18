using System;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader.Config;
using TerrariaProgression.Config;
using TerrariaProgression.Core;
using TerrariaProgression.UI;

namespace ProgressionHarness;
public sealed partial class RuntimeChecks
{
    private void RunLoadoutInput()
    {
        bool wasServer=Main.dedServ,focus=Main.hasFocus,menu=Main.gameMenu,paused=Main.gamePaused;
        var oldInput=Main.inputText;var oldPrevious=Main.oldInputText;
        bool writing=PlayerInput.WritingText,blocked=Main.blockInput,options=Main.ingameOptionsWindow,fancy=Main.inFancyUI;
        bool chat=Main.drawingPlayerChat,sign=Main.editSign,chest=Main.editChest,visible=TalentUISystem.Visible;
        var textOwner=Main.CurrentInputTextTakerOverride;
        try {
            Reset();Config.GlobalTalentLevelLimit="-1";Config.TalentLevelOverrides.Clear();Config.OnChanged();
            foreach(string valid in new[]{"Shift+1","shift + d1","Ctrl+F1","Alt+Q","Ctrl+Shift+2","F6","NumPad1","Shift+0"})
                Check(LoadoutChord.TryParse(valid,out _),"parse supported chord "+valid);
            foreach(string invalid in new[]{"", " ", "Shift", "Shift++1", "Shift+Shift+1", "A+B", "Ctrl+999", "Ctrl+LeftShift", "Windows+1", "Shift+Maybe", "+F1", "Control+Ctrl+1", "161"})
                Check(!LoadoutChord.TryParse(invalid,out _),"reject invalid chord "+invalid);
            LoadoutChord.TryParse("Shift+1",out var chord);
            var down=new KeyboardState(Keys.LeftShift,Keys.D1);
            Check(chord.Pressed(down,default),"Shift+1 fires on primary key edge");
            Check(chord.Held(new KeyboardState(Keys.RightShift,Keys.D1)),"right shift is equivalent");
            Check(!chord.Held(new KeyboardState(Keys.D1)),"plain 1 does not activate shift chord");
            Check(!chord.Held(new KeyboardState(Keys.LeftShift,Keys.LeftControl,Keys.D1)),"extra modifiers do not match simpler chord");
            Check(!chord.Pressed(down,down)&&!chord.Pressed(down,new KeyboardState(Keys.D1)),"held key and modifier-after-key do not retrigger");
            var cfg=new LoadoutHotkeyConfig();
            Check(cfg.Mode==ConfigScope.ClientSide&&cfg.Enabled&&cfg.Shortcuts.Count==10&&cfg.Shortcuts[9].Shortcut=="Shift+0","ten local-only defaults include shift zero");
            cfg.Shortcuts.Clear();cfg.Shortcuts.Add(new(){LoadoutNumber=2,Shortcut="Ctrl+F1"});
            var restored=JsonConvert.DeserializeObject<LoadoutHotkeyConfig>(JsonConvert.SerializeObject(cfg))!;
            Check(restored.Shortcuts.Count==1&&restored.Shortcuts[0].LoadoutNumber==2&&restored.Shortcuts[0].Shortcut=="Ctrl+F1","saved custom list replaces defaults instead of duplicating");
            cfg.Shortcuts.Clear();restored=JsonConvert.DeserializeObject<LoadoutHotkeyConfig>(JsonConvert.SerializeObject(cfg))!;
            Check(restored.Shortcuts.Count==0,"deliberately cleared bindings remain empty after reload");
            cfg.Shortcuts.Add(new(){Shortcut="invalid"});cfg.Shortcuts.Add(new(){Shortcut=""});LoadoutHotkeySystem.Configure(cfg);
            Check(LoadoutHotkeySystem.InvalidEntries.SequenceEqual(new[]{1}),"invalid entry identified but blank binding is a deliberate disable");
            A.Award(Experience.Cost(1,100,50000));
            Guid first=A.State.ActiveLoadoutId;
            A.ApplyTalent(TalentOperation.Upgrade,"Damage",TalentCategory.Combat,100);
            A.ApplyTalent(TalentOperation.CreateLoadout,"采矿",TalentCategory.BaseStats,1);Guid second=A.State.ActiveLoadoutId;
            A.ApplyTalent(TalentOperation.Upgrade,"PickPower",TalentCategory.Utility,100);
            LoadoutHotkeySystem.Configure(new());
            var revision=A.TalentRevision;
            Check(LoadoutHotkeySystem.HandleBindings(down,default,A)&&A.State.ActiveLoadoutId==first&&A.TalentRevision==revision+1,"default chord selects existing first allocation by authoritative transaction");
            Check(NumericTalents.ActiveLevel(A.State,"Damage")==100&&NumericTalents.ActiveLevel(A.State,"PickPower")==0,"keyboard activation has same exclusive effects as UI");
            Check(!LoadoutHotkeySystem.HandleBindings(down,down,A)&&A.TalentRevision==revision+1,"held chord sends no repeat request");
            Check(!LoadoutHotkeySystem.HandleBindings(new KeyboardState(Keys.LeftShift,Keys.D9),default,A)&&A.State.Loadouts.Count==2,"missing ordinal creates nothing");
            Check(!LoadoutHotkeySystem.TrySwitch(A,1)&&!LoadoutHotkeySystem.TrySwitch(A,0),"active and invalid ordinal are no-ops");
            cfg.Shortcuts.Clear();cfg.Shortcuts.Add(new(){LoadoutNumber=2,Shortcut="Ctrl+F1"});cfg.Shortcuts.Add(new(){LoadoutNumber=1,Shortcut="Ctrl+F1"});LoadoutHotkeySystem.Configure(cfg);
            Check(LoadoutHotkeySystem.HandleBindings(new KeyboardState(Keys.RightControl,Keys.F1),default,A)&&A.State.ActiveLoadoutId==second,"custom chord uses right control and deterministic first duplicate");
            A.PendingRequest=1;Check(!LoadoutHotkeySystem.TrySwitch(A,1)&&A.State.ActiveLoadoutId==second,"pending network response prevents shortcut overwrite");A.PendingRequest=0;
            A.Player.dead=true;Check(!LoadoutHotkeySystem.TrySwitch(A,1),"death rejects direct shortcut switch");A.Player.dead=false;
            A.SessionReady=false;Check(!LoadoutHotkeySystem.TrySwitch(A,1),"unready character rejects shortcut");A.SessionReady=true;
            cfg.Enabled=false;LoadoutHotkeySystem.Configure(cfg);
            Check(!LoadoutHotkeySystem.HandleBindings(new KeyboardState(Keys.LeftControl,Keys.F1),default,A),"global local toggle disables shortcuts");
            A.ApplyTalent(TalentOperation.DeleteLoadout,first.ToString("N"),TalentCategory.BaseStats,1);
            A.ApplyTalent(TalentOperation.CreateLoadout,"第三套",TalentCategory.BaseStats,1);
            Check(LoadoutHotkeySystem.TrySwitch(A,1)&&A.State.ActiveLoadoutId==second,"ordinal follows visible order after deleting earlier page");
            // Use native trigger tables to ensure Shift+number does not also select a held item.
            var mapping=PlayerInput.CurrentProfile.InputModes[InputMode.Keyboard].KeyStatus;
            var oldKeys=mapping["Hotbar1"];
            try {
                mapping["Hotbar1"]=new(){"D1"};PlayerInput.Triggers.Current.KeyStatus["Hotbar1"]=true;PlayerInput.Triggers.JustPressed.KeyStatus["Hotbar1"]=true;
                LoadoutHotkeySystem.SuppressHotbar(Keys.D1);
                Check(!PlayerInput.Triggers.Current.Hotbar1&&!PlayerInput.Triggers.JustPressed.Hotbar1,"matching vanilla hotbar trigger suppressed in both tables");
                PlayerInput.Triggers.Current.KeyStatus["Hotbar1"]=true;LoadoutHotkeySystem.SuppressHotbar(Keys.F6);
                Check(PlayerInput.Triggers.Current.Hotbar1,"unrelated key leaves hotbar input intact");
            } finally {mapping["Hotbar1"]=oldKeys;PlayerInput.Triggers.Current.KeyStatus["Hotbar1"]=false;}
            Main.dedServ=false;Main.hasFocus=true;Main.gameMenu=Main.gamePaused=Main.ingameOptionsWindow=Main.inFancyUI=false;
            Main.drawingPlayerChat=Main.editSign=Main.editChest=Main.blockInput=PlayerInput.WritingText=TalentUISystem.Visible=false;
            Main.CurrentInputTextTakerOverride=null;
            Check(LoadoutHotkeySystem.ContextAllowed,"living focused gameplay allows shortcuts");
            foreach(int condition in Enumerable.Range(0,11)) {
                Main.drawingPlayerChat=condition==0;Main.editSign=condition==1;Main.editChest=condition==2;
                PlayerInput.WritingText=condition==3;Main.blockInput=condition==4;Main.ingameOptionsWindow=condition==5;
                Main.inFancyUI=condition==6;Main.gamePaused=condition==7;Main.gameMenu=condition==8;Main.hasFocus=condition!=9;
                Main.CurrentInputTextTakerOverride=condition==10?new object():null;
                Check(!LoadoutHotkeySystem.ContextAllowed,"typing/menu/focus gate "+condition);
            }
            Main.hasFocus=true;Main.CurrentInputTextTakerOverride=null;
            Main.inputText=Main.oldInputText=default;
            // Real native character queue / GetInputText, including committed Unicode.
            void Characters(string value){Main.keyCount=value.Length;for(int i=0;i<value.Length;i++){Main.keyInt[i]=value[i];Main.keyString[i]=value[i].ToString();}}
            Main.CurrentInputTextTakerOverride=new object();Characters("字");Main.OpenPlayerChat();
            Check(!Main.drawingPlayerChat&&Main.keyCount==1,"text owner prevents Enter opening chat and clearing character queue");Main.CurrentInputTextTakerOverride=null;
            Characters("战斗");Check(TalentUIState.ReadText("",32)=="战斗"&&Main.keyCount==0,"native text reader accepts committed Chinese and consumes once");
            Characters("abc12");Check(TalentUIState.ReadText("采矿",32)=="采矿abc12","native reader accepts letters and numbers after Chinese");
            Characters("新增");Check(TalentUIState.ReadText(new string('a',31),32)==new string('a',31)+"新","name limit preserves complete characters");
            Characters("😀");Check(TalentUIState.ReadText(new string('a',31),32)==new string('a',31),"length clipping does not split surrogate pairs");
            Characters("X");Main.hasFocus=false;Check(TalentUIState.ReadText("原名",32)=="原名","native reader leaves name intact while unfocused");
        }
        finally {
            Main.dedServ=wasServer;Main.hasFocus=focus;Main.gameMenu=menu;Main.gamePaused=paused;
            Main.ingameOptionsWindow=options;Main.inFancyUI=fancy;Main.drawingPlayerChat=chat;Main.editSign=sign;Main.editChest=chest;
            Main.blockInput=blocked;PlayerInput.WritingText=writing;TalentUISystem.Visible=visible;Main.CurrentInputTextTakerOverride=textOwner;
            Main.inputText=oldInput;Main.oldInputText=oldPrevious;Main.clrInput();
            LoadoutHotkeySystem.Configure(new());Reset();Config.OnChanged();
        }
    }
}
