using Godot;

public static class SceneRouteNavigator
{
    public static void Navigate(SceneRoute route, SceneTree tree)
    {
        var router = tree.Root.GetNodeOrNull<SceneRouter>("/root/SceneRouter");
        if (router is not null)
        {
            router.Navigate(route);
            return;
        }

        ChangeScene(route, tree);
    }

    internal static void ChangeScene(SceneRoute route, SceneTree tree)
    {
        if (route == SceneRoute.Quit)
        {
            tree.Quit();
            return;
        }

        var scenePath = SceneRoutePaths.Resolve(route);
        if (scenePath is null)
        {
            return;
        }

        var err = tree.ChangeSceneToFile(scenePath);
        if (err != Error.Ok)
        {
            GD.PrintErr($"Cambio scena fallito: {scenePath} ({err})");
        }
    }
}
