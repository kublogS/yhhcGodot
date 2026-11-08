using Godot;

public partial class HudOverlayController : Control
{
    private GameSession _session = null!;
    private Panel _panel = null!;
    private Label _name = null!;
    private Label _hp = null!;
    private ProgressBar _hpBar = null!;
    private Label _mana = null!;
    private ProgressBar _manaBar = null!;
    private Label _exp = null!;
    private ProgressBar _expBar = null!;
    private Label _soli = null!;
    private Label _enemies = null!;
    private string? _statusOverride;
    private ulong _statusUntilMs;

    public override void _Ready()
    {
        Theme = GameUiThemeFactory.GetOrCreate();
        _session = GetNode<GameSession>("/root/GameSession");
        _panel = GetNode<Panel>("Panel");
        _name = GetNode<Label>("Panel/VBox/Name");
        _hp = GetNode<Label>("Panel/VBox/Hp");
        _hpBar = GetNode<ProgressBar>("Panel/VBox/HpBar");
        _mana = GetNode<Label>("Panel/VBox/Mana");
        _manaBar = GetNode<ProgressBar>("Panel/VBox/ManaBar");
        _exp = GetNode<Label>("Panel/VBox/Exp");
        _expBar = GetNode<ProgressBar>("Panel/VBox/ExpBar");
        _soli = GetNode<Label>("Panel/VBox/Soli");
        _enemies = GetNode<Label>("Panel/VBox/Enemies");
        ApplyReadableHudStyle();
    }

    public void UpdateFromState(CharacterModel player, int overworldEnemies)
    {
        var expNeeded = Mathf.Max(1, _session.XpNeededForLevel(Mathf.Max(1, player.Level)));
        var expValue = Mathf.Clamp(player.Exp, 0, expNeeded);
        _name.Text = player.Name;
        _hp.Text = $"HP {player.Hp}/{player.MaxHp}";
        _hpBar.MaxValue = Mathf.Max(1, player.MaxHp);
        _hpBar.Value = Mathf.Clamp(player.Hp, 0, player.MaxHp);
        _mana.Text = $"MN {player.Mana}/{player.MaxMana}";
        _manaBar.MaxValue = Mathf.Max(1, player.MaxMana);
        _manaBar.Value = Mathf.Clamp(player.Mana, 0, player.MaxMana);
        _exp.Text = $"EXP {expValue}/{expNeeded}  •  Lv {Mathf.Max(1, player.Level)}";
        _expBar.MaxValue = expNeeded;
        _expBar.Value = expValue;
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
        _exp.Modulate = bright;
        _soli.Modulate = new Color(0.82f, 0.9f, 0.98f);
        _enemies.Modulate = bright;
        ApplyBarStyle(_hpBar, new Color(0.84f, 0.24f, 0.28f));
        ApplyBarStyle(_manaBar, new Color(0.22f, 0.49f, 0.9f));
        ApplyBarStyle(_expBar, new Color(0.42f, 0.76f, 0.35f));
    }

    private static void ApplyBarStyle(ProgressBar bar, Color fillColor)
    {
        bar.ShowPercentage = false;
        bar.AddThemeStyleboxOverride("fill", new StyleBoxFlat
        {
            BgColor = fillColor,
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        });
        bar.AddThemeStyleboxOverride("background", new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.08f, 0.11f, 0.95f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        });
    }
}
