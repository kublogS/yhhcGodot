using Godot;

public partial class HudOverlayController : Control
{
    private Panel _panel = null!;
    private Label _name = null!;
    private Label _hp = null!;
    private Label _mana = null!;
    private Label _soli = null!;
    private Label _enemies = null!;
    private string? _statusOverride;
    private ulong _statusUntilMs;

    public override void _Ready()
    {
        Theme = GameUiThemeFactory.GetOrCreate();
        _panel = GetNode<Panel>("Panel");
        _name = GetNode<Label>("Panel/VBox/Name");
        _hp = GetNode<Label>("Panel/VBox/Hp");
        _mana = GetNode<Label>("Panel/VBox/Mana");
        _soli = GetNode<Label>("Panel/VBox/Soli");
        _enemies = GetNode<Label>("Panel/VBox/Enemies");
        ApplyReadableHudStyle();
    }

    public void UpdateFromState(CharacterModel player, int overworldEnemies)
    {
        _name.Text = player.Name;
        _hp.Text = $"HP {player.Hp}/{player.MaxHp}";
        _mana.Text = $"Mana {player.Mana}/{player.MaxMana}";
        _soli.Text = $"Soli {player.Soli}";
        if (_statusOverride is not null && Time.GetTicksMsec() < _statusUntilMs)
        {
            _enemies.Text = _statusOverride;
        }
        else
        {
            _statusOverride = null;
            _enemies.Text = $"Nemici area {overworldEnemies}";
        }
    }

    public void ShowTransientStatus(string message, float seconds = 2.4f)
    {
        _statusOverride = message;
        _statusUntilMs = Time.GetTicksMsec() + (ulong)(Mathf.Max(0.2f, seconds) * 1000f);
    }

    private void ApplyReadableHudStyle()
    {
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.07f, 0.11f, 0.16f, 0.82f),
            BorderColor = new Color(0.8f, 0.9f, 1f, 0.9f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 7,
            CornerRadiusTopRight = 7,
            CornerRadiusBottomLeft = 7,
            CornerRadiusBottomRight = 7,
            ShadowColor = new Color(0f, 0f, 0f, 0.36f),
            ShadowSize = 5,
        };
        _panel.AddThemeStyleboxOverride("panel", panelStyle);
        _panel.Modulate = Colors.White;

        var bright = new Color(0.93f, 0.96f, 1f);
        var accent = new Color(0.9f, 0.96f, 1f);
        _name.Modulate = accent;
        _hp.Modulate = bright;
        _mana.Modulate = bright;
        _soli.Modulate = new Color(0.82f, 0.9f, 0.98f);
        _enemies.Modulate = bright;
    }
}
