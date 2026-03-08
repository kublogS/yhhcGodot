using System.Collections.Generic;
using Godot;

public partial class BattleController : Control
{
    private RichTextLabel _log = null!;
    private readonly List<Button> _moveButtons = new();
    private readonly List<Button> _targetButtons = new();
    private BattleFlowCoordinator _flow = null!;
    private int _selectedTarget;

    private TextureRect _backdrop = null!;
    private ColorRect _backdropFallback = null!;
    private Label _playerName = null!;
    private Label _enemyName = null!;
    private TextureRect _playerPortrait = null!;
    private TextureRect _enemyPortrait = null!;

    public override void _Ready()
    {
        Theme = GameUiThemeFactory.GetOrCreate();
        _flow = new BattleFlowCoordinator(GameSession.Instance);
        _log = GetNode<RichTextLabel>("Root/Log");
        _backdrop = GetNode<TextureRect>("Backdrop");
        _backdropFallback = GetNode<ColorRect>("BackdropFallback");
        var shade = GetNode<ColorRect>("Shade");
        _playerName = GetNode<Label>("Root/Visuals/PlayerPanel/Box/PlayerName");
        _enemyName = GetNode<Label>("Root/Visuals/EnemyPanel/Box/EnemyName");
        _playerPortrait = GetNode<TextureRect>("Root/Visuals/PlayerPanel/Box/PlayerSprite");
        _enemyPortrait = GetNode<TextureRect>("Root/Visuals/EnemyPanel/Box/EnemySprite");
        _backdropFallback.Color = PythonColorPalette.Black;
        shade.Color = PythonColorPalette.OverlayBlack(180);
        _playerName.Modulate = PythonColorPalette.Title;
        _enemyName.Modulate = PythonColorPalette.Title;

        BindButtons();
        BindStaticAssets();
        Refresh();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
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
            ExecuteTurn(new CombatTurnRequest { ActionType = CombatActionType.Items, SelectedItemId = "CureMedie" });
        }
        else if (@event.IsActionPressed("battle_flee"))
        {
            ExecuteTurn(new CombatTurnRequest { ActionType = CombatActionType.Flee });
        }
        else if (@event.IsActionPressed("ui_cancel"))
        {
            SceneRouteNavigator.Navigate(SceneRoute.Quit, GetTree());
        }
    }

    private void BindButtons()
    {
        _moveButtons.Add(GetNode<Button>("Root/Actions/Moves/Move0"));
        _moveButtons.Add(GetNode<Button>("Root/Actions/Moves/Move1"));
        _moveButtons.Add(GetNode<Button>("Root/Actions/Moves/Move2"));
        _moveButtons.Add(GetNode<Button>("Root/Actions/Moves/Move3"));
        _moveButtons.Add(GetNode<Button>("Root/Actions/Moves/Move4"));

        _targetButtons.Add(GetNode<Button>("Root/Targets/Target1"));
        _targetButtons.Add(GetNode<Button>("Root/Targets/Target2"));
        _targetButtons.Add(GetNode<Button>("Root/Targets/Target3"));
        _targetButtons.Add(GetNode<Button>("Root/Targets/Target4"));

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

        GetNode<Button>("Root/Actions/Defend").Pressed += () => ExecuteTurn(new CombatTurnRequest { ActionType = CombatActionType.Defend });
        GetNode<Button>("Root/Actions/Items").Pressed += () => ExecuteTurn(new CombatTurnRequest { ActionType = CombatActionType.Items, SelectedItemId = "CureMedie" });
        GetNode<Button>("Root/Actions/Flee").Pressed += () => ExecuteTurn(new CombatTurnRequest { ActionType = CombatActionType.Flee });
    }

    private void BindStaticAssets()
    {
        _backdrop.Texture = GameAssets.LoadBattleBackground();
        _backdropFallback.Visible = _backdrop.Texture is null;
    }

    private void ExecuteTurn(CombatTurnRequest request)
    {
        var result = _flow.ExecuteTurn(request);
        foreach (var line in result.LogLines)
        {
            AppendLog(line);
        }

        if (result.Route != SceneRoute.None)
        {
            SceneRouteNavigator.Navigate(result.Route, GetTree());
            return;
        }

        Refresh();
    }

    private void Refresh()
    {
        var view = _flow.BuildScreenState(_selectedTarget);
        if (view is null)
        {
            SceneRouteNavigator.Navigate(SceneRoute.MainMenu, GetTree());
            return;
        }

        var state = GameSession.Instance.State;
        if (state is not null && state.Enemies.Count > 0)
        {
            _selectedTarget = Mathf.Clamp(_selectedTarget, 0, state.Enemies.Count - 1);
        }
        else
        {
            _selectedTarget = 0;
        }

        for (var i = 0; i < _moveButtons.Count; i++)
        {
            _moveButtons[i].Text = view.MoveLabels[i];
        }

        for (var i = 0; i < _targetButtons.Count; i++)
        {
            var model = view.Targets[i];
            _targetButtons[i].Visible = model.Visible;
            _targetButtons[i].Text = model.Label;
            _targetButtons[i].Modulate = model.Selected ? PythonColorPalette.Title : PythonColorPalette.ButtonText;
        }

        RefreshPortraits();
    }

    private void RefreshPortraits()
    {
        var state = GameSession.Instance.State;
        if (state is null)
        {
            _playerName.Text = "Player";
            _enemyName.Text = "Enemy";
            _playerPortrait.Texture = GameAssets.LoadPlayerSprite();
            _enemyPortrait.Texture = GameAssets.LoadEnemySprite("enemy");
            return;
        }

        _playerName.Text = state.Player.Name;
        _playerPortrait.Texture = GameAssets.LoadCharacterSprite(state.Player.Sprite, enemy: false);

        if (state.Enemies.Count == 0)
        {
            _enemyName.Text = "Nessun nemico";
            _enemyPortrait.Texture = GameAssets.LoadEnemySprite("enemy");
            return;
        }

        var enemyIdx = Mathf.Clamp(_selectedTarget, 0, state.Enemies.Count - 1);
        var enemy = state.Enemies[enemyIdx];
        _enemyName.Text = enemy.Name;
        _enemyPortrait.Texture = GameAssets.LoadCharacterSprite(enemy.Sprite, enemy: true);
    }

    private void AppendLog(string text)
    {
        _log.Text += $"{text}\n";
        _log.ScrollToLine(_log.GetLineCount());
    }
}
