using Godot;

public sealed class BattlePlayerStatusHud
{
    private readonly GameSession _session;
    private readonly PanelContainer _statsPanel;
    private readonly Label _name;
    private readonly Label _hpLabel;
    private readonly ProgressBar _hpBar;
    private readonly Label _manaLabel;
    private readonly ProgressBar _manaBar;
    private readonly Label _expLabel;
    private readonly ProgressBar _expBar;

    public BattlePlayerStatusHud(Control root, GameSession session)
    {
        _session = session;
        _statsPanel = root.GetNode<PanelContainer>("Root/CombatRow/HudPanel/HudRoot/Hud/PlayerStatsPanel");
        _name = root.GetNode<Label>("Root/CombatRow/HudPanel/HudRoot/Hud/PlayerStatsPanel/PlayerStats/PlayerName");
        _hpLabel = root.GetNode<Label>("Root/CombatRow/HudPanel/HudRoot/Hud/PlayerStatsPanel/PlayerStats/PlayerHpLabel");
        _hpBar = root.GetNode<ProgressBar>("Root/CombatRow/HudPanel/HudRoot/Hud/PlayerStatsPanel/PlayerStats/PlayerHpBar");
        _manaLabel = root.GetNode<Label>("Root/CombatRow/HudPanel/HudRoot/Hud/PlayerStatsPanel/PlayerStats/PlayerManaLabel");
        _manaBar = root.GetNode<ProgressBar>("Root/CombatRow/HudPanel/HudRoot/Hud/PlayerStatsPanel/PlayerStats/PlayerManaBar");
        _expLabel = root.GetNode<Label>("Root/CombatRow/HudPanel/HudRoot/Hud/PlayerStatsPanel/PlayerStats/PlayerExpLabel");
        _expBar = root.GetNode<ProgressBar>("Root/CombatRow/HudPanel/HudRoot/Hud/PlayerStatsPanel/PlayerStats/PlayerExpBar");
        ApplyStyle();
    }

    public void Refresh(GameState? state)
    {
        if (state is null)
        {
            _name.Text = "Player";
            _hpLabel.Text = "HP 0/0";
            _manaLabel.Text = "MN 0/0";
            _expLabel.Text = "EXP 0/1  •  Lv 1";
            _hpBar.MaxValue = 1;
            _hpBar.Value = 0;
            _manaBar.MaxValue = 1;
            _manaBar.Value = 0;
            _expBar.MaxValue = 1;
            _expBar.Value = 0;
            return;
        }

        var player = state.Player;
        var expNeeded = Mathf.Max(1, _session.XpNeededForLevel(Mathf.Max(1, player.Level)));
        var expValue = Mathf.Clamp(player.Exp, 0, expNeeded);
        _name.Text = player.Name;
        _hpLabel.Text = $"HP {player.Hp}/{player.MaxHp}";
        _manaLabel.Text = $"MN {player.Mana}/{player.MaxMana}";
        _expLabel.Text = $"EXP {expValue}/{expNeeded}  •  Lv {Mathf.Max(1, player.Level)}";
        _hpBar.MaxValue = Mathf.Max(1, player.MaxHp);
        _hpBar.Value = Mathf.Clamp(player.Hp, 0, player.MaxHp);
        _manaBar.MaxValue = Mathf.Max(1, player.MaxMana);
        _manaBar.Value = Mathf.Clamp(player.Mana, 0, player.MaxMana);
        _expBar.MaxValue = expNeeded;
        _expBar.Value = expValue;
    }

    private void ApplyStyle()
    {
        _statsPanel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.92f, 0.95f, 1f, 0.16f),
            BorderColor = new Color(0.78f, 0.84f, 0.95f, 0.8f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
        });

        _name.HorizontalAlignment = HorizontalAlignment.Left;
        _hpLabel.HorizontalAlignment = HorizontalAlignment.Left;
        _manaLabel.HorizontalAlignment = HorizontalAlignment.Left;
        _expLabel.HorizontalAlignment = HorizontalAlignment.Left;
        _name.Modulate = new Color(0.95f, 0.97f, 1f);
        _hpLabel.Modulate = new Color(0.95f, 0.97f, 1f);
        _manaLabel.Modulate = new Color(0.95f, 0.97f, 1f);
        _expLabel.Modulate = new Color(0.95f, 0.97f, 1f);
        ApplyBarStyle(_hpBar, new Color(0.84f, 0.24f, 0.28f));
        ApplyBarStyle(_manaBar, new Color(0.24f, 0.49f, 0.9f));
        ApplyBarStyle(_expBar, new Color(0.45f, 0.77f, 0.34f));
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
            BgColor = new Color(0.06f, 0.08f, 0.11f, 0.95f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4,
        });
    }
}
