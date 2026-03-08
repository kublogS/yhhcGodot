using Godot;

public static class SceneRouteNavigator
{
    public static void Navigate(SceneRoute route, SceneTree tree)
    {
        switch (route)
        {
            case SceneRoute.MainMenu:
                SceneRouter.Instance.GoToMainMenu();
                break;
            case SceneRoute.SavesMenu:
                SceneRouter.Instance.GoToSavesMenu();
                break;
            case SceneRoute.NameEntry:
                SceneRouter.Instance.GoToNameEntry();
                break;
            case SceneRoute.Explore:
                SceneRouter.Instance.GoToExplore();
                break;
            case SceneRoute.Battle:
                SceneRouter.Instance.GoToBattle();
                break;
            case SceneRoute.Reward:
                SceneRouter.Instance.GoToReward();
                break;
            case SceneRoute.Quit:
                tree.Quit();
                break;
        }
    }
}
