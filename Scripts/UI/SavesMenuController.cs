using System.Collections.Generic;
using Godot;

public partial class SavesMenuController : Control
{
    private readonly List<Button> _slotButtons = new();
    private readonly List<Button> _deleteButtons = new();
    private SaveSlotFlowCoordinator _flow = null!;
    private int _index;

    public override void _Ready()
    {
        _flow = new SaveSlotFlowCoordinator(GameSession.Instance, SaveService.Instance);
        BindButtons();
        Refresh();
        FocusCurrent();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
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
            DeleteSlot(_index);
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
        GetNode<Button>("Center/Back").Pressed += () => SceneRouteNavigator.Navigate(SceneRoute.MainMenu, GetTree());

        for (var i = 0; i < _slotButtons.Count; i++)
        {
            var idx = i;
            _slotButtons[i].Pressed += () => SceneRouteNavigator.Navigate(_flow.SelectSlotByIndex(idx), GetTree());
            _deleteButtons[i].Pressed += () => DeleteSlot(idx);
        }
    }

    private void Refresh()
    {
        var slots = _flow.ListSlots();
        for (var i = 0; i < _slotButtons.Count; i++)
        {
            var slot = slots[i];
            _slotButtons[i].Text = slot.IsUsed
                ? $"Slot {slot.SlotId} | {slot.CharacterName} | Floor {slot.DeepestFloor} | {slot.LastSaveText}"
                : $"Slot {slot.SlotId} | Nuova partita";
            _deleteButtons[i].Disabled = !slot.IsUsed;
        }
    }

    private void DeleteSlot(int index)
    {
        _flow.DeleteSlotByIndex(index);
        Refresh();
        FocusCurrent();
    }

    private void FocusCurrent()
    {
        _slotButtons[_index].GrabFocus();
    }
}
