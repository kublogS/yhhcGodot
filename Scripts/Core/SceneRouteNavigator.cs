using Godot;

public static class SceneRouteNavigator
{
    public static void Navigate(SceneRoute route, SceneTree tree)
    {
        var router = tree.Root.GetNodeOrNull<SceneRouter>("/root/SceneRouter");
        if (router is null)
        {
            NavigateWithoutRouter(route, tree);
            return;
        }

        switch (route)
        {
            case SceneRoute.MainMenu:
                router.GoToMainMenu();
                break;
            case SceneRoute.SavesMenu:
                router.GoToSavesMenu();
                break;
            case SceneRoute.NameEntry:
                router.GoToNameEntry();
                break;
            case SceneRoute.Explore:
                router.GoToExplore();
                break;
            case SceneRoute.Battle:
                router.GoToBattle();
                break;
            case SceneRoute.Reward:
                router.GoToReward();
                break;
            case SceneRoute.Quit:
                tree.Quit();
                break;
        }
    }

    private static void NavigateWithoutRouter(SceneRoute route, SceneTree tree)
    {
        if (route == SceneRoute.Quit)
        {
            tree.Quit();
            return;
        }

        var scenePath = route switch
        {
            SceneRoute.MainMenu => "res://Scenes/MainMenu.tscn",
            SceneRoute.SavesMenu => "res://Scenes/SavesMenu.tscn",
            SceneRoute.NameEntry => "res://Scenes/NameEntry.tscn",
            SceneRoute.Explore => "res://Scenes/Explore.tscn",
            SceneRoute.Battle => "res://Scenes/Battle.tscn",
            SceneRoute.Reward => "res://Scenes/Reward.tscn",
            _ => string.Empty,
        };
        if (string.IsNullOrWhiteSpace(scenePath))
        {
            return;
        }

        var err = tree.ChangeSceneToFile(scenePath);
        if (err != Error.Ok)
        {
            GD.PrintErr($"Cambio scena fallback fallito: {scenePath} ({err})");
        }
    }
}
