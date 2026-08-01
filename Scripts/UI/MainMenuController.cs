using Godot;

public partial class MainMenuController : Control
{
    private SceneRouter _sceneRouter = null!;

    public override void _Ready()
    {
        _sceneRouter = GetNode<SceneRouter>("/root/SceneRouter");
        Theme = GameUiThemeFactory.GetOrCreate();
        var background = GetNode<TextureRect>("Background");
        var fallback = GetNode<ColorRect>("Fallback");
        var shade = GetNode<ColorRect>("Shade");

        background.Texture = GameAssets.LoadMainMenuWallpaper();
        fallback.Visible = background.Texture is null;
        fallback.Color = new Color(0.42f, 0.56f, 0.49f, 1f);
        shade.Color = PythonColorPalette.OverlayBlack(105);

        GetNode<Button>("Center/VBox/PartiteButton").Pressed += () => _sceneRouter.GoToSavesMenu();
        GetNode<Button>("Center/VBox/EsciButton").Pressed += () => GetTree().Quit();
    }
}
