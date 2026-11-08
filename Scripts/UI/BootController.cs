using System;
using Godot;

// Autoloads (Database, SaveService, ...) are already _Ready by the time the
// main scene runs, so data and save slots are loaded; Boot only hands off.
public partial class BootController : Node
{
    public override void _Ready()
    {
        CallDeferred(nameof(BootFlow));
    }

    private void BootFlow()
    {
        try
        {
            SceneRouteNavigator.Navigate(SceneRoute.MainMenu, GetTree());
        }
        catch (Exception ex)
        {
            GD.PushError($"Errore in boot: {ex}");
            SceneRouteNavigator.ChangeScene(SceneRoute.MainMenu, GetTree());
        }
    }
}
