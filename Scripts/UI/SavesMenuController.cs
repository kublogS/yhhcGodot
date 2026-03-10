using System.Collections.Generic;
using Godot;

public partial class SavesMenuController : Control
{
    private Label _title = null!;
    private Label _status = null!;
    private ConfirmationDialog _deleteConfirm = null!;
    private ConfirmationDialog _overwriteConfirm = null!;
    private readonly List<Button> _slotButtons = new();
    private readonly List<Button> _deleteButtons = new();
    private readonly List<Button> _copyButtons = new();
    private readonly List<SaveSlotView> _slots = new();
    private SaveSlotFlowCoordinator _flow = null!;
    private int _index;
    private int _copySourceIndex = -1;
    private int _pendingDeleteIndex = -1;
    private int _pendingOverwriteSourceIndex = -1;
    private int _pendingOverwriteTargetIndex = -1;

    public override void _Ready()
    {
        Theme = GameUiThemeFactory.GetOrCreate();
        _flow = new SaveSlotFlowCoordinator(GameSession.Instance, SaveService.Instance);
        _title = GetNode<Label>("Center/VBox/Title");
        _status = GetNode<Label>("Center/VBox/Status");
        _deleteConfirm = GetNode<ConfirmationDialog>("DeleteConfirm");
        _overwriteConfirm = GetNode<ConfirmationDialog>("OverwriteConfirm");
        _title.Modulate = PythonColorPalette.SaveSlotTitle;
        _status.Modulate = PythonColorPalette.Muted;
        _deleteConfirm.Confirmed += ConfirmDelete;
        _overwriteConfirm.Confirmed += ConfirmOverwriteCopy;
        BindButtons();
        SetStatus("Seleziona slot: Enter carica, Elimina cancella, Duplica copia.");
        Refresh();
        FocusCurrent();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_deleteConfirm.Visible || _overwriteConfirm.Visible)
        {
            return;
        }

        if (@event.IsActionPressed("slot_up"))
        {
            _index = (_index + _slotButtons.Count - 1) % _slotButtons.Count;
            FocusCurrent();
        }
        else if (@event.IsActionPressed("slot_down"))
        {
            _index = (_index + 1) % _slotButtons.Count;
            FocusCurrent();
        }
        else if (@event.IsActionPressed("slot_select"))
        {
            SceneRouteNavigator.Navigate(_flow.SelectSlotByIndex(_index), GetTree());
        }
        else if (@event.IsActionPressed("slot_delete"))
        {
            AskDeleteSlot(_index);
        }
        else if (@event.IsActionPressed("slot_copy"))
        {
            PressCopy(_index);
        }
        else if (@event.IsActionPressed("ui_cancel"))
        {
            SceneRouteNavigator.Navigate(SceneRoute.MainMenu, GetTree());
        }
    }

    private void BindButtons()
    {
        _slotButtons.Add(GetNode<Button>("Center/VBox/Slot1/Select"));
        _slotButtons.Add(GetNode<Button>("Center/VBox/Slot2/Select"));
        _slotButtons.Add(GetNode<Button>("Center/VBox/Slot3/Select"));
        _deleteButtons.Add(GetNode<Button>("Center/VBox/Slot1/Delete"));
        _deleteButtons.Add(GetNode<Button>("Center/VBox/Slot2/Delete"));
        _deleteButtons.Add(GetNode<Button>("Center/VBox/Slot3/Delete"));
        _copyButtons.Add(GetNode<Button>("Center/VBox/Slot1/Copy"));
        _copyButtons.Add(GetNode<Button>("Center/VBox/Slot2/Copy"));
        _copyButtons.Add(GetNode<Button>("Center/VBox/Slot3/Copy"));
        GetNode<Button>("Center/Back").Pressed += () => SceneRouteNavigator.Navigate(SceneRoute.MainMenu, GetTree());

        for (var i = 0; i < _slotButtons.Count; i++)
        {
            var idx = i;
            _slotButtons[i].Pressed += () => SceneRouteNavigator.Navigate(_flow.SelectSlotByIndex(idx), GetTree());
            _deleteButtons[i].Pressed += () => AskDeleteSlot(idx);
            _copyButtons[i].Pressed += () => PressCopy(idx);
        }
    }

    private void Refresh()
    {
        _slots.Clear();
        _slots.AddRange(_flow.ListSlots());
        for (var i = 0; i < _slotButtons.Count; i++)
        {
            var slot = _slots[i];
            _slotButtons[i].Text = slot.IsUsed
                ? $"Slot {slot.SlotId} | {slot.CharacterName} | Floor {slot.DeepestFloor} | {slot.LastSaveText}"
                : $"Slot {slot.SlotId} | Nuova partita";
            _deleteButtons[i].Disabled = !slot.IsUsed;
            _copyButtons[i].Disabled = _copySourceIndex < 0 ? !slot.IsUsed : false;
            _copyButtons[i].Text = _copySourceIndex == i ? "Annulla" : (_copySourceIndex >= 0 ? "Incolla" : "Duplica");
            _slotButtons[i].Modulate = i == _index ? PythonColorPalette.Title : PythonColorPalette.Text;
            _deleteButtons[i].Modulate = PythonColorPalette.Muted;
            _copyButtons[i].Modulate = PythonColorPalette.Muted;
            ApplySlotTheme(_slotButtons[i], slot.IsUsed, i == _index || i == _copySourceIndex);
        }
    }

    private void FocusCurrent()
    {
        _slotButtons[_index].GrabFocus();
        Refresh();
    }

    private bool IsUsedSlot(int index)
    {
        return index >= 0 && index < _slots.Count && _slots[index].IsUsed;
    }

    private void SetStatus(string message)
    {
        _status.Text = message;
    }
}
