using Godot;

public partial class ColorThemeService : Node
{
    public static ColorThemeService Instance { get; private set; } = null!;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        ApplyGlobalTheme();
    }

    public void ApplyGlobalTheme()
    {
        var root = GetTree().Root;
        if (root is null)
        {
            return;
        }

        root.Theme = GameUiThemeFactory.GetOrCreate();
    }
}
