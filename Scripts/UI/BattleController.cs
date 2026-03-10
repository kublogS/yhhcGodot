using System.Collections.Generic;
using Godot;

public partial class BattleController : Control
{
    private RichTextLabel _log = null!;
    private readonly List<Button> _moveButtons = new();
    private readonly List<Button> _targetButtons = new();
    private readonly List<Button> _actionButtons = new();
    private BattleFlowCoordinator _flow = null!;
    private int _selectedTarget;

    private TextureRect _backdrop = null!;
    private ColorRect _backdropFallback = null!;
    private Label _enemyName = null!;
    private TextureRect _enemyPortrait = null!;
    private PanelContainer _hudPanel = null!;
    private TextureRect _paperGrain = null!;
    private ColorRect _glossBand = null!;
    private ColorRect _playerHitFlash = null!;
    private ColorRect _enemyHitFlash = null!;

    public override void _Ready()
    {
        Theme = GameUiThemeFactory.GetOrCreate();
        _flow = new BattleFlowCoordinator(GameSession.Instance);
        Input.MouseMode = Input.MouseModeEnum.Visible;

        _log = GetNode<RichTextLabel>("Root/CombatRow/HudPanel/HudRoot/Hud/Log");
        _backdrop = GetNode<TextureRect>("Backdrop");
        _backdropFallback = GetNode<ColorRect>("BackdropFallback");
        var shade = GetNode<ColorRect>("Shade");
        _enemyName = GetNode<Label>("Root/CombatRow/EnemyPanel/EnemyBox/EnemyName");
        _enemyPortrait = GetNode<TextureRect>("Root/CombatRow/EnemyPanel/EnemyBox/EnemySprite");
        _hudPanel = GetNode<PanelContainer>("Root/CombatRow/HudPanel");
        _paperGrain = GetNode<TextureRect>("Root/CombatRow/HudPanel/HudRoot/PaperGrain");
        _glossBand = GetNode<ColorRect>("Root/CombatRow/HudPanel/HudRoot/Gloss");
        _playerHitFlash = GetNode<ColorRect>("PlayerHitFlash");
        _enemyHitFlash = GetNode<ColorRect>("Root/CombatRow/EnemyPanel/EnemyBox/EnemySprite/EnemyHitFlash");

        _backdropFallback.Color = PythonColorPalette.Black;
        shade.Color = PythonColorPalette.OverlayBlack(180);
        _enemyName.Modulate = PythonColorPalette.Title;

        BindButtons();
        BindBackdrop();
        ConfigureHudPresentation();
        ConfigureNavigation();
        ConfigureDamageFeedback();
        Refresh();
        FocusDefaultCommand();
    }

    private async void ExecuteTurn(CombatTurnRequest request)
    {
        if (IsPlaybackLocked())
        {
            return;
        }

        SetPlaybackLock(true);
        try
        {
            var result = _flow.ExecuteTurn(request);
            await PlayEventSequence(result.Events, result.LogLines);
            if (result.Route != SceneRoute.None)
            {
                SceneRouteNavigator.Navigate(result.Route, GetTree());
                return;
            }

            Refresh();
        }
        finally
        {
            SetPlaybackLock(false);
        }
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
            var button = _targetButtons[i];
            button.Visible = model.Visible;
            button.Disabled = !model.Visible;
            button.FocusMode = model.Visible ? Control.FocusModeEnum.All : Control.FocusModeEnum.None;
            button.Text = model.Label;
            button.Modulate = model.Selected ? PythonColorPalette.Title : PythonColorPalette.ButtonText;
            button.SelfModulate = Colors.White;
        }

        RefreshEnemyVisual();
        RefreshFocusAfterUiUpdate();
    }

    private void RefreshEnemyVisual()
    {
        var state = GameSession.Instance.State;
        if (state is null || state.Enemies.Count == 0)
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
