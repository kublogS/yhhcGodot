using System.Linq;
using Godot;

public partial class BattleController
{
    public override void _UnhandledInput(InputEvent @event)
    {
        if (IsPlaybackLocked())
        {
            if (@event.IsPressed())
            {
                AcceptEvent();
            }

            return;
        }

        if (HandleSecondaryPointerInput(@event))
        {
            AcceptEvent();
            return;
        }

        if (@event.IsActionPressed("battle_attack"))
        {
            ExecuteTurn(new CombatTurnRequest { ActionType = CombatActionType.Attack, SelectedMoveIndex = 0, SelectedTargetIndex = _selectedTarget });
            AcceptEvent();
            return;
        }

        if (@event.IsActionPressed("battle_defend"))
        {
            ExecuteTurn(new CombatTurnRequest { ActionType = CombatActionType.Defend });
            AcceptEvent();
            return;
        }

        if (@event.IsActionPressed("battle_items"))
        {
            ExecuteTurn(new CombatTurnRequest { ActionType = CombatActionType.Items, SelectedItemId = "CureMedie" });
            AcceptEvent();
            return;
        }

        if (@event.IsActionPressed("battle_flee"))
        {
            ExecuteTurn(new CombatTurnRequest { ActionType = CombatActionType.Flee });
            AcceptEvent();
            return;
        }

        if (@event.IsActionPressed("ui_cancel"))
        {
            HandleSecondaryBackNavigation();
            AcceptEvent();
            return;
        }

        if (@event.IsActionPressed("ui_accept") && GetViewport().GuiGetFocusOwner() is Button focused && !focused.Disabled)
        {
            focused.EmitSignal(Button.SignalName.Pressed);
            AcceptEvent();
        }
    }

    private void BindButtons()
    {
        _moveButtons.Add(GetNode<Button>("Root/CombatRow/HudPanel/HudRoot/Hud/Actions/Moves/Move0"));
        _moveButtons.Add(GetNode<Button>("Root/CombatRow/HudPanel/HudRoot/Hud/Actions/Moves/Move1"));
        _moveButtons.Add(GetNode<Button>("Root/CombatRow/HudPanel/HudRoot/Hud/Actions/Moves/Move2"));
        _moveButtons.Add(GetNode<Button>("Root/CombatRow/HudPanel/HudRoot/Hud/Actions/Moves/Move3"));
        _moveButtons.Add(GetNode<Button>("Root/CombatRow/HudPanel/HudRoot/Hud/Actions/Moves/Move4"));

        _targetButtons.Add(GetNode<Button>("Root/CombatRow/HudPanel/HudRoot/Hud/Targets/Target1"));
        _targetButtons.Add(GetNode<Button>("Root/CombatRow/HudPanel/HudRoot/Hud/Targets/Target2"));
        _targetButtons.Add(GetNode<Button>("Root/CombatRow/HudPanel/HudRoot/Hud/Targets/Target3"));
        _targetButtons.Add(GetNode<Button>("Root/CombatRow/HudPanel/HudRoot/Hud/Targets/Target4"));

        var defend = GetNode<Button>("Root/CombatRow/HudPanel/HudRoot/Hud/Actions/ActionBar/Defend");
        var items = GetNode<Button>("Root/CombatRow/HudPanel/HudRoot/Hud/Actions/ActionBar/Items");
        var flee = GetNode<Button>("Root/CombatRow/HudPanel/HudRoot/Hud/Actions/ActionBar/Flee");
        _actionButtons.Add(defend);
        _actionButtons.Add(items);
        _actionButtons.Add(flee);

        for (var i = 0; i < _moveButtons.Count; i++)
        {
            var idx = i;
            _moveButtons[i].Pressed += () => ExecuteTurn(new CombatTurnRequest
            {
                ActionType = CombatActionType.Attack,
                SelectedMoveIndex = idx,
                SelectedTargetIndex = _selectedTarget,
            });
        }

        for (var i = 0; i < _targetButtons.Count; i++)
        {
            var idx = i;
            _targetButtons[i].Pressed += () =>
            {
                _selectedTarget = idx;
                Refresh();
            };
        }

        defend.Pressed += () => ExecuteTurn(new CombatTurnRequest { ActionType = CombatActionType.Defend });
        items.Pressed += () => ExecuteTurn(new CombatTurnRequest { ActionType = CombatActionType.Items, SelectedItemId = "CureMedie" });
        flee.Pressed += () => ExecuteTurn(new CombatTurnRequest { ActionType = CombatActionType.Flee });
    }

    private void ConfigureNavigation()
    {
        foreach (var button in AllInteractiveButtons())
        {
            button.FocusMode = Control.FocusModeEnum.All;
            button.MouseEntered += button.GrabFocus;
        }

        WireRowNavigation(_targetButtons);
        WireRowNavigation(_moveButtons);
        WireRowNavigation(_actionButtons);
        WireVerticalNavigation(_targetButtons, _moveButtons);
        WireVerticalNavigation(_moveButtons, _actionButtons);
    }

    private void FocusDefaultCommand()
    {
        var defaultButton = _moveButtons.FirstOrDefault(button => button.Visible && !button.Disabled) ?? _actionButtons.First();
        defaultButton.GrabFocus();
    }

    private void RefreshFocusAfterUiUpdate()
    {
        if (GetViewport().GuiGetFocusOwner() is not Button focused)
        {
            FocusDefaultCommand();
            return;
        }

        if (!focused.IsVisibleInTree() || focused.Disabled)
        {
            FocusDefaultCommand();
        }
    }

    private bool HandleSecondaryPointerInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouse || !mouse.Pressed || mouse.ButtonIndex != MouseButton.Right)
        {
            return false;
        }

        HandleSecondaryBackNavigation();
        return true;
    }

    private void HandleSecondaryBackNavigation()
    {
        var focusOwner = GetViewport().GuiGetFocusOwner() as Button;
        if (focusOwner is not null && _targetButtons.Contains(focusOwner))
        {
            FocusDefaultCommand();
            return;
        }

        var target = _targetButtons.FirstOrDefault(button => button.Visible && !button.Disabled);
        if (target is not null)
        {
            target.GrabFocus();
            return;
        }

        FocusDefaultCommand();
    }
}
