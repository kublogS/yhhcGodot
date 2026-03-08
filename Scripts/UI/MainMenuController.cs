using Godot;

public partial class MainMenuController : Control
{
    public override void _Ready()
    {
        Theme = GameUiThemeFactory.GetOrCreate();
        var background = GetNode<TextureRect>("Background");
        var fallback = GetNode<ColorRect>("Fallback");
        var shade = GetNode<ColorRect>("Shade");
        var title = GetNode<Label>("Center/VBox/Title");

        background.Texture = GameAssets.LoadMainMenuWallpaper();
        fallback.Visible = background.Texture is null;
        fallback.Color = PythonColorPalette.Black;
        shade.Color = PythonColorPalette.OverlayBlack(170);
        title.Modulate = PythonColorPalette.Title;

        GetNode<Button>("Center/VBox/PartiteButton").Pressed += () => SceneRouter.Instance.GoToSavesMenu();
        GetNode<Button>("Center/VBox/EsciButton").Pressed += () => GetTree().Quit();
    }
}
