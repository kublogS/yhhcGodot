using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BattleController : Control
{
    private GameSession _session = null!;
    private BattleFlowCoordinator _flow = null!;
    private RichTextLabel _log = null!;
    private TextureRect _backdrop = null!;
    private ColorRect _backdropFallback = null!;
    private BattleEnemyPanelHud _enemyPanelHud = null!;
    private BattlePlayerStatusHud _playerStatusHud = null!;
    private BattleOverlayViewState _viewState = null!;
    private readonly List<Button> _moveButtons = new();
    private readonly List<Button> _actionButtons = new();
    private int _selectedTarget;
    private bool _embeddedMode;
    private bool _turnLocked;

    public event Action<BattleFlowOutcome>? OutcomeResolved;
    public event Action<int>? TargetChanged;
    public Func<CombatEventEntry, Task>? EventPresenter { get; set; }

    public override void _Ready()
    {
        _session = GetNode<GameSession>("/root/GameSession");
        Theme = GameUiThemeFactory.GetOrCreate();
        _flow = new BattleFlowCoordinator(_session, new BattleAnalyticsLogSink());
        _log = GetNode<RichTextLabel>("Root/CombatRow/HudPanel/HudRoot/TopRightLog");
        _backdrop = GetNode<TextureRect>("Backdrop");
        _backdropFallback = GetNode<ColorRect>("BackdropFallback");
        var shade = GetNode<ColorRect>("Shade");
        _enemyPanelHud = new BattleEnemyPanelHud(this, SelectTarget);
        _playerStatusHud = new BattlePlayerStatusHud(this, _session);
        _viewState = new BattleOverlayViewState(_session, _log, _backdrop, _backdropFallback, shade, _moveButtons, _actionButtons, _enemyPanelHud);
        BindButtons();
        if (Visible)
        {
            _embeddedMode = false;
            if (!Activate())
            {
                HandleOutcome(BattleFlowOutcome.GoToMainMenu);
            }
            return;
        }
        Deactivate();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible || _turnLocked)
        {
            return;
        }

        if (@event.IsActionPressed("battle_attack"))
        {
            ExecuteTurn(new CombatTurnRequest { ActionType = CombatActionType.Attack, SelectedMoveIndex = 0, SelectedTargetIndex = _selectedTarget });
        }
        else if (@event.IsActionPressed("battle_defend"))
        {
            ExecuteTurn(new CombatTurnRequest { ActionType = CombatActionType.Defend });
        }
        else if (@event.IsActionPressed("battle_items"))
        {
            ExecuteTurn(new CombatTurnRequest { ActionType = CombatActionType.Items });
        }
        else if (@event.IsActionPressed("battle_flee"))
        {
            ExecuteTurn(new CombatTurnRequest { ActionType = CombatActionType.Flee });
        }
    }

    public bool OpenOverlay() { _embeddedMode = true; return Activate(); }

    public void CloseOverlay() { Deactivate(); }

    private bool Activate()
    {
        if (_session.State is null)
        {
            return false;
        }

        _viewState.Activate(this, useWorldPresentation: _embeddedMode);
        _selectedTarget = 0;
        _turnLocked = false;
        Refresh();
        if (_moveButtons.Count > 0)
        {
            _moveButtons[0].GrabFocus();
        }
        return true;
    }

    private void Deactivate()
    {
        _turnLocked = false;
        _viewState?.Deactivate(this);
    }

    private void SelectTarget(int index)
    {
        if (_turnLocked) return;
        _selectedTarget = index;
        Refresh();
    }

    private void BindButtons()
    {
        for (var i = 0; i < 5; i++)
        {
            var button = GetNode<Button>($"Root/CombatRow/HudPanel/HudRoot/Hud/ActionsRow/AttackGrid/Move{i}");
            var index = i;
            button.Pressed += () => ExecuteTurn(new CombatTurnRequest
            {
                ActionType = CombatActionType.Attack,
                SelectedMoveIndex = index,
                SelectedTargetIndex = _selectedTarget,
            });
            _moveButtons.Add(button);
        }

        BindUtilityButton("Root/CombatRow/HudPanel/HudRoot/Hud/ActionsRow/UtilityRow/Defend", CombatActionType.Defend);
        BindUtilityButton("Root/CombatRow/HudPanel/HudRoot/Hud/ActionsRow/UtilityRow/Items", CombatActionType.Items);
        BindUtilityButton("Root/CombatRow/HudPanel/HudRoot/Hud/ActionsRow/UtilityRow/Flee", CombatActionType.Flee);
    }

    private void BindUtilityButton(string path, CombatActionType actionType)
    {
        var button = GetNode<Button>(path);
        button.Pressed += () => ExecuteTurn(new CombatTurnRequest { ActionType = actionType });
        _actionButtons.Add(button);
    }

    private async void ExecuteTurn(CombatTurnRequest request)
    {
        if (_turnLocked)
        {
            return;
        }
        _turnLocked = true;
        _viewState.SetButtonsEnabled(false);
        try
        {
            var result = await BattleTurnPlayback.ExecuteAsync(this, _flow, request, _viewState, EventPresenter);
            if (result.Outcome != BattleFlowOutcome.ContinueBattle)
            {
                HandleOutcome(result.Outcome);
                return;
            }
            Refresh();
        }
        finally
        {
            _turnLocked = false;
            _viewState.SetButtonsEnabled(true);
        }
    }

    private void Refresh()
    {
        var view = _flow.BuildScreenState(_selectedTarget);
        if (view is null)
        {
            HandleOutcome(BattleFlowOutcome.GoToMainMenu);
            return;
        }
        for (var i = 0; i < _moveButtons.Count; i++)
        {
            _moveButtons[i].Text = view.MoveLabels[i];
        }
        _playerStatusHud.Refresh(_session.State);
        _selectedTarget = _enemyPanelHud.Refresh(_session.State, _selectedTarget);
        TargetChanged?.Invoke(_selectedTarget);
    }

    private void HandleOutcome(BattleFlowOutcome outcome)
    {
        if (_embeddedMode)
        {
            OutcomeResolved?.Invoke(outcome);
            return;
        }
        SceneRouteNavigator.Navigate(BattleFlowOutcomeRouting.ToSceneRoute(outcome), GetTree());
    }
}
