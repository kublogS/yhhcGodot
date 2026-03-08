using Godot;

public partial class MainMenuController : Control
{
    public override void _Ready()
    {
        var background = GetNode<TextureRect>("Background");
        var fallback = GetNode<ColorRect>("Fallback");

        background.Texture = GameAssets.LoadMainMenuWallpaper();
        fallback.Visible = background.Texture is null;

        GetNode<Button>("Center/VBox/PartiteButton").Pressed += () => SceneRouter.Instance.GoToSavesMenu();
        GetNode<Button>("Center/VBox/EsciButton").Pressed += () => GetTree().Quit();
    }
}
