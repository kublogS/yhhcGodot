using System.Collections.Generic;
using Godot;

public partial class SavesMenuController : Control
{
    private GameSession _session = null!;
    private SaveService _saveService = null!;
    private TextureRect _background = null!;
    private ColorRect _fallback = null!;
    private ColorRect _tint = null!;
    private ColorRect _bottomShade = null!;
    private Label _title = null!;
    private Label _status = null!;
    private ConfirmationDialog _deleteConfirm = null!;
    private ConfirmationDialog _overwriteConfirm = null!;
    private readonly List<PanelContainer> _slotCards = new();
    private readonly List<Label> _slotNameLabels = new();
    private readonly List<Label> _slotInfoLabels = new();
    private readonly List<Label> _slotLastSaveLabels = new();
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
        _session = GetNode<GameSession>("/root/GameSession");
        _saveService = GetNode<SaveService>("/root/SaveService");
        _flow = new SaveSlotFlowCoordinator(_session, _saveService);
        _background = GetNode<TextureRect>("Background");
        _fallback = GetNode<ColorRect>("Fallback");
        _tint = GetNode<ColorRect>("Tint");
        _bottomShade = GetNode<ColorRect>("BottomShade");
        _title = GetNode<Label>("Layout/Root/Head/Title");
        _status = GetNode<Label>("Layout/Root/Head/Status");
        _deleteConfirm = GetNode<ConfirmationDialog>("DeleteConfirm");
        _overwriteConfirm = GetNode<ConfirmationDialog>("OverwriteConfirm");
        _deleteConfirm.Confirmed += ConfirmDelete;
        _overwriteConfirm.Confirmed += ConfirmOverwriteCopy;
        ConfigureBackdropStyle();
        BindButtons();
        SetStatus("Seleziona uno slot: Enter continua/nuova partita.");
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
        BindSlot("Layout/Root/Cards/Slot1Card");
        BindSlot("Layout/Root/Cards/Slot2Card");
        BindSlot("Layout/Root/Cards/Slot3Card");
        GetNode<Button>("Layout/Root/Bottom/Back").Pressed += () => SceneRouteNavigator.Navigate(SceneRoute.MainMenu, GetTree());

        for (var i = 0; i < _slotButtons.Count; i++)
        {
            var idx = i;
            _slotButtons[i].Pressed += () => SceneRouteNavigator.Navigate(_flow.SelectSlotByIndex(idx), GetTree());
            _deleteButtons[i].Pressed += () => AskDeleteSlot(idx);
            _copyButtons[i].Pressed += () => PressCopy(idx);
        }
    }

    private void BindSlot(string cardPath)
    {
        _slotCards.Add(GetNode<PanelContainer>(cardPath));
        _slotNameLabels.Add(GetNode<Label>($"{cardPath}/CardMargin/Card/SlotLabel"));
        _slotInfoLabels.Add(GetNode<Label>($"{cardPath}/CardMargin/Card/InfoLabel"));
        _slotLastSaveLabels.Add(GetNode<Label>($"{cardPath}/CardMargin/Card/LastSaveLabel"));
        _slotButtons.Add(GetNode<Button>($"{cardPath}/CardMargin/Card/Select"));
        _deleteButtons.Add(GetNode<Button>($"{cardPath}/CardMargin/Card/Actions/Delete"));
        _copyButtons.Add(GetNode<Button>($"{cardPath}/CardMargin/Card/Actions/Copy"));
    }

    private void Refresh()
    {
        _slots.Clear();
        _slots.AddRange(_flow.ListSlots());
        _index = Mathf.Clamp(_index, 0, _slotButtons.Count - 1);
        for (var i = 0; i < _slotButtons.Count; i++)
        {
            var slot = _slots[i];
            var shortName = slot.CharacterName.Length > 16 ? $"{slot.CharacterName[..16]}…" : slot.CharacterName;
            _slotNameLabels[i].Text = $"SLOT {slot.SlotId}";
            _slotInfoLabels[i].Text = slot.IsUsed
                ? $"{shortName}\nPiano max {slot.DeepestFloor} • Labirinti {slot.LabyrinthCompletions}"
                : "Nuova partita\nPiano max - • Labirinti -";
            _slotLastSaveLabels[i].Text = slot.IsUsed
                ? $"Ultimo save: {slot.LastSaveText}"
                : "Nessun salvataggio";
            _slotButtons[i].Text = slot.IsUsed ? "Continua" : "Nuova partita";
            _slotNameLabels[i].Modulate = new Color(0.86f, 0.95f, 0.9f, 1f);
            _slotInfoLabels[i].Modulate = new Color(0.76f, 0.87f, 0.81f, 1f);
            _slotLastSaveLabels[i].Modulate = new Color(0.64f, 0.76f, 0.7f, 1f);
            _deleteButtons[i].Disabled = !slot.IsUsed;
            _copyButtons[i].Disabled = _copySourceIndex < 0 ? !slot.IsUsed : false;
            _copyButtons[i].Text = _copySourceIndex == i ? "Annulla" : (_copySourceIndex >= 0 ? "Incolla" : "Duplica");
            ApplySlotTheme(
                _slotCards[i],
                _slotButtons[i],
                _deleteButtons[i],
                _copyButtons[i],
                slot.IsUsed,
                i == _index,
                i == _copySourceIndex || (_copySourceIndex >= 0 && i != _copySourceIndex));
        }
    }

    private void FocusCurrent()
    {
        _index = Mathf.Clamp(_index, 0, _slotButtons.Count - 1);
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

    private void ConfigureBackdropStyle()
    {
        _background.Texture = GameAssets.LoadMainMenuWallpaper();
        _fallback.Visible = _background.Texture is null;
        _fallback.Color = new Color(0.3f, 0.38f, 0.34f, 1f);
        _tint.Color = new Color(0.14f, 0.22f, 0.18f, 0.56f);
        _bottomShade.Color = new Color(0.02f, 0.05f, 0.04f, 0.82f);
        _title.Modulate = new Color(0.86f, 0.95f, 0.9f, 1f);
        _status.Modulate = new Color(0.69f, 0.81f, 0.74f, 1f);
    }
}
