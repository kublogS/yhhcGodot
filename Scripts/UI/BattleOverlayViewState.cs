using System.Collections.Generic;
using Godot;

public sealed class BattleOverlayViewState
{
    private readonly GameSession _session;
    private readonly RichTextLabel _log;
    private readonly TextureRect _backdrop;
    private readonly ColorRect _backdropFallback;
    private readonly ColorRect _shade;
    private readonly IReadOnlyList<Button> _moveButtons;
    private readonly IReadOnlyList<Button> _actionButtons;
    private readonly BattleEnemyPanelHud _enemyPanelHud;

    public BattleOverlayViewState(
        GameSession session,
        RichTextLabel log,
        TextureRect backdrop,
        ColorRect backdropFallback,
        ColorRect shade,
        IReadOnlyList<Button> moveButtons,
        IReadOnlyList<Button> actionButtons,
        BattleEnemyPanelHud enemyPanelHud)
    {
        _session = session;
        _log = log;
        _backdrop = backdrop;
        _backdropFallback = backdropFallback;
        _shade = shade;
        _moveButtons = moveButtons;
        _actionButtons = actionButtons;
        _enemyPanelHud = enemyPanelHud;
    }

    public void Activate(Control root, bool useWorldPresentation)
    {
        root.Visible = true;
        root.ProcessMode = Node.ProcessModeEnum.Inherit;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        SetButtonsEnabled(true);
        AppendLog(string.Empty);
        BindBackdrop(useWorldPresentation);
    }

    public void Deactivate(Control root)
    {
        root.Visible = false;
        root.ProcessMode = Node.ProcessModeEnum.Disabled;
        _backdrop.Texture = null;
        _backdrop.Visible = false;
        _backdropFallback.Visible = false;
        _shade.Visible = false;
        AppendLog(string.Empty);
        SetButtonsEnabled(false);
    }

    public void AppendLog(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _log.Text = string.Empty;
            return;
        }

        _log.Text += $"[right]{text}[/right]\n";
        _log.ScrollToLine(_log.GetLineCount());
    }

    public void SetButtonsEnabled(bool enabled)
    {
        foreach (var button in _moveButtons) button.Disabled = !enabled;
        foreach (var button in _actionButtons) button.Disabled = !enabled;
        _enemyPanelHud.SetEnabled(enabled);
    }

    private void BindBackdrop(bool useWorldPresentation)
    {
        if (useWorldPresentation)
        {
            _backdrop.Visible = false;
            _backdropFallback.Visible = false;
            _shade.Visible = false;
            return;
        }

        var contextual = _session.ConsumeBattleBackdrop();
        _backdrop.Texture = contextual ?? GameAssets.LoadBattleBackground();
        _backdrop.Visible = true;
        _backdropFallback.Visible = _backdrop.Texture is null;
        _backdropFallback.Color = PythonColorPalette.Black;
        _shade.Visible = true;
    }
}
