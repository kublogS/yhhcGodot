using System.Collections.Generic;
using Godot;

public partial class SavesMenuController : Control
{
    private Label _title = null!;
    private readonly List<Button> _slotButtons = new();
    private readonly List<Button> _deleteButtons = new();
    private SaveSlotFlowCoordinator _flow = null!;
    private int _index;

    public override void _Ready()
    {
        Theme = GameUiThemeFactory.GetOrCreate();
        _flow = new SaveSlotFlowCoordinator(GameSession.Instance, SaveService.Instance);
        _title = GetNode<Label>("Center/VBox/Title");
        _title.Modulate = PythonColorPalette.SaveSlotTitle;
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
            _slotButtons[i].Modulate = i == _index ? PythonColorPalette.Title : PythonColorPalette.Text;
            _deleteButtons[i].Modulate = PythonColorPalette.Muted;
            ApplySlotTheme(_slotButtons[i], slot.IsUsed, i == _index);
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
        Refresh();
    }

    private static void ApplySlotTheme(Button button, bool isUsed, bool isSelected)
    {
        var fill = isUsed ? PythonColorPalette.SaveSlotFill : PythonColorPalette.ButtonBg;
        var border = isSelected ? PythonColorPalette.Title : PythonColorPalette.PanelBorder;
        button.AddThemeStyleboxOverride("normal", BuildSlotStyle(fill, border, 1));
        button.AddThemeStyleboxOverride("hover", BuildSlotStyle(fill, PythonColorPalette.Title, 2));
        button.AddThemeStyleboxOverride("pressed", BuildSlotStyle(PythonColorPalette.Gray, PythonColorPalette.Title, 2));
        button.AddThemeStyleboxOverride("focus", BuildSlotStyle(fill, PythonColorPalette.Title, 2));
    }

    private static StyleBoxFlat BuildSlotStyle(Color fill, Color border, int width)
    {
        return new StyleBoxFlat
        {
            BgColor = fill,
            BorderColor = border,
            BorderWidthTop = width,
            BorderWidthBottom = width,
            BorderWidthLeft = width,
            BorderWidthRight = width,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
        };
    }
}
