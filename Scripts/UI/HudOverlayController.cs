using Godot;

public partial class HudOverlayController : Control
{
    private Label _name = null!;
    private Label _hp = null!;
    private Label _mana = null!;
    private Label _soli = null!;
    private Label _enemies = null!;

    public override void _Ready()
    {
        _name = GetNode<Label>("Panel/VBox/Name");
        _hp = GetNode<Label>("Panel/VBox/Hp");
        _mana = GetNode<Label>("Panel/VBox/Mana");
        _soli = GetNode<Label>("Panel/VBox/Soli");
        _enemies = GetNode<Label>("Panel/VBox/Enemies");
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
