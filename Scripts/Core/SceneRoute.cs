public enum SceneRoute
{
    None = 0,
    MainMenu,
    SavesMenu,
    NameEntry,
    Explore,
    Battle,
    Reward,
    Quit,
}

// Single source of truth for route -> scene file. SceneRouter and the
// router-less fallback in SceneRouteNavigator both resolve through here.
public static class SceneRoutePaths
{
    public static string? Resolve(SceneRoute route)
    {
        return route switch
        {
            SceneRoute.MainMenu => "res://Scenes/MainMenu.tscn",
            SceneRoute.SavesMenu => "res://Scenes/SavesMenu.tscn",
            SceneRoute.NameEntry => "res://Scenes/NameEntry.tscn",
            SceneRoute.Explore => "res://Scenes/Explore.tscn",
            SceneRoute.Battle => "res://Scenes/Battle.tscn",
            SceneRoute.Reward => "res://Scenes/Reward.tscn",
            _ => null,
        };
    }
}
