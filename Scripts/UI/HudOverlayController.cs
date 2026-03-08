using Godot;

public partial class HudOverlayController : Control
{
    private Panel _panel = null!;
    private Label _name = null!;
    private Label _hp = null!;
    private Label _mana = null!;
    private Label _soli = null!;
    private Label _enemies = null!;

    public override void _Ready()
    {
        Theme = GameUiThemeFactory.GetOrCreate();
        _panel = GetNode<Panel>("Panel");
        _name = GetNode<Label>("Panel/VBox/Name");
        _hp = GetNode<Label>("Panel/VBox/Hp");
        _mana = GetNode<Label>("Panel/VBox/Mana");
        _soli = GetNode<Label>("Panel/VBox/Soli");
        _enemies = GetNode<Label>("Panel/VBox/Enemies");
        _panel.Modulate = PythonColorPalette.WithAlpha(PythonColorPalette.HudBg, 220);
        _name.Modulate = PythonColorPalette.Title;
        _hp.Modulate = PythonColorPalette.Text;
        _mana.Modulate = PythonColorPalette.Text;
        _soli.Modulate = PythonColorPalette.Muted;
        _enemies.Modulate = PythonColorPalette.Text;
    }

    public void UpdateFromState(CharacterModel player, int overworldEnemies)
    {
        _name.Text = player.Name;
        _hp.Text = $"HP {player.Hp}/{player.MaxHp}";
        _mana.Text = $"Mana {player.Mana}/{player.MaxMana}";
        _soli.Text = $"Soli {player.Soli}";
        _enemies.Text = $"Nemici area {overworldEnemies}";
    }
}
