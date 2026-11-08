using Godot;

public partial class SceneRouter : Node
{
    public static SceneRouter Instance { get; private set; } = null!;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public void GoToMainMenu() => Navigate(SceneRoute.MainMenu);
    public void GoToSavesMenu() => Navigate(SceneRoute.SavesMenu);
    public void GoToNameEntry() => Navigate(SceneRoute.NameEntry);
    public void GoToExplore() => Navigate(SceneRoute.Explore);
    public void GoToBattle() => Navigate(SceneRoute.Battle);
    public void GoToReward() => Navigate(SceneRoute.Reward);

    public void Navigate(SceneRoute route)
    {
        SceneRouteNavigator.ChangeScene(route, GetTree());
    }
}
