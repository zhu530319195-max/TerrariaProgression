using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;
using TerrariaProgression.Core;

namespace TerrariaProgression.UI;

internal sealed partial class TalentUIState
{
    private UIPanel? loadoutOverlay;
    private TalentButton? loadoutNameInput;
    private bool loadoutNameFocused;
    private string loadoutName = "";
    private Guid editedLoadout;
    private TalentOperation loadoutOperation;
    internal bool IsTyping => SearchFocused || loadoutNameFocused;

    private void InitializeLoadouts()
    {
        var choose = new TalentButton(() => Text("LoadoutCurrent", Player.State.ActiveLoadout.Name), OpenLoadoutList, () => CanAct);
        choose.Left.Set(0,0); choose.Top.Set(62,0); choose.Width.Set(-8,.44f); panel.Append(choose);
        var operations = new[] { TalentOperation.CreateLoadout, TalentOperation.CopyLoadout, TalentOperation.RenameLoadout, TalentOperation.DeleteLoadout };
        for (int i = 0; i < operations.Length; i++) {
            var operation = operations[i];
            var button = new TalentButton(() => Text(operation.ToString()), () => OpenLoadoutEditor(operation),
                () => CanAct && (operation != TalentOperation.DeleteLoadout || Player.State.Loadouts.Count > 1));
            button.Left.Set(0,.44f+i*.14f); button.Top.Set(62,0); button.Width.Set(-4,.14f); panel.Append(button);
        }
    }
    private UIPanel OpenLoadoutOverlay()
    {
        EndSearch();
        // Cover the whole panel so clicks cannot reach an underlying talent row.
        var overlay = new UIPanel { BackgroundColor = new Color(20,29,48,253), BorderColor = new Color(176,146,84) };
        overlay.Width.Set(0,1); overlay.Height.Set(0,1); overlay.SetPadding(16);
        panel.Append(overlay); loadoutOverlay = overlay;
        return overlay;
    }
    private void OpenLoadoutList()
    {
        var overlay = OpenLoadoutOverlay();
        var caption = new UIText(Text("LoadoutChoose"),.85f) { TextOriginX=0, DynamicallyScaleDownToWidth=true };
        Place(caption,overlay,0,0,1,0,30);
        var area = new UIElement(); area.Top.Set(40,0); area.Width.Set(0,1); area.Height.Set(-90,1); overlay.Append(area);
        var rows = new UIList(); AttachList(area,rows);
        foreach (var page in Player.State.Loadouts) {
            var entry = new TalentButton(() => Text("LoadoutEntry",page.Name,Compact(page.AvailablePoints),Compact(page.SpentPoints)),
                () => { Player.RequestTalent(TalentOperation.ActivateLoadout,page.Id.ToString("N")); CloseLoadoutOverlay(); },
                () => CanManageLoadouts, () => page.Id == Player.State.ActiveLoadoutId);
            entry.Width.Set(0,1); entry.TextHAlign=0; rows.Add(entry);
        }
        var cancel = Button("LoadoutCancel",CloseLoadoutOverlay); cancel.Top.Set(-34,1); cancel.Width.Set(120,0); overlay.Append(cancel);
        overlay.Recalculate();
    }
    private void OpenLoadoutEditor(TalentOperation operation)
    {
        var overlay = OpenLoadoutOverlay();
        editedLoadout = Player.State.ActiveLoadoutId;
        loadoutOperation = operation;
        var caption = new UIText(Text(operation.ToString()),.9f) { TextOriginX=0 };
        Place(caption,overlay,0,4,1,0,32);
        var explanation = new UIText(operation == TalentOperation.DeleteLoadout ? Text("LoadoutDeleteConfirm",Player.State.ActiveLoadout.Name) : Text("LoadoutNameHelp"),.8f)
            { IsWrapped=true, TextOriginX=0, DynamicallyScaleDownToWidth=true };
        Place(explanation,overlay,0,44,1,0,90);
        if (operation != TalentOperation.DeleteLoadout) {
            loadoutName = operation == TalentOperation.CreateLoadout ? Text("LoadoutDefault",Player.State.Loadouts.Count+1) : Player.State.ActiveLoadout.Name;
            loadoutNameInput = new TalentButton(() => loadoutName + " |", () => {}, () => true);
            Place(loadoutNameInput,overlay,0,150,1,0,36);
            loadoutNameFocused=true; Main.clrInput(); Main.blockInput=true;
        }
        var confirm = Button("LoadoutConfirm",ConfirmLoadout, () => CanManageLoadouts && editedLoadout == Player.State.ActiveLoadoutId
            && (operation == TalentOperation.DeleteLoadout || TalentLoadouts.ValidName(loadoutName.Trim())));
        confirm.Top.Set(-42,1); confirm.Width.Set(-8,.5f); overlay.Append(confirm);
        var cancel = Button("LoadoutCancel",CloseLoadoutOverlay);
        cancel.Left.Set(0,.5f); cancel.Top.Set(-42,1); cancel.Width.Set(0,.5f); overlay.Append(cancel);
        overlay.Recalculate();
    }
    private void ConfirmLoadout()
    {
        if (!CanManageLoadouts || editedLoadout != Player.State.ActiveLoadoutId) return;
        if (loadoutOperation != TalentOperation.DeleteLoadout && !TalentLoadouts.ValidName(loadoutName.Trim())) return;
        Player.RequestTalent(loadoutOperation,loadoutOperation == TalentOperation.DeleteLoadout ? editedLoadout.ToString("N") : loadoutName.Trim());
        CloseLoadoutOverlay();
    }
    private void CloseLoadoutOverlay()
    {
        loadoutOverlay?.Remove(); loadoutOverlay=null; loadoutNameInput=null;
        if (loadoutNameFocused) { loadoutNameFocused=false; Main.blockInput=false; PlayerInput.WritingText=false; }
    }
    private void UpdateLoadoutInput()
    {
        if (!loadoutNameFocused) return;
        Main.blockInput=true; PlayerInput.WritingText=true; Main.instance.HandleIME();
        string input=Main.GetInputText(loadoutName);
        if (input.Length > TalentLoadouts.MaxNameLength) {
            input=input[..TalentLoadouts.MaxNameLength];
            if (char.IsHighSurrogate(input[^1])) input=input[..^1];
        }
        loadoutName=input;
        // Confirmation is an explicit click: Enter remains available to Chinese IME.
    }
    private void DrawLoadoutIME()
    {
        if (loadoutNameFocused && loadoutNameInput != null)
            Main.instance.DrawWindowsIMEPanel(loadoutNameInput.GetDimensions().Position()+new Vector2(0,36),1);
    }
}
