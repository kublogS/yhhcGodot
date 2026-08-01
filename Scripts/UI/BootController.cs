using System;
using Godot;

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
            var database = GetNodeOrNull<Database>("/root/Database");
            database?.LoadAll();

            var saveService = GetNodeOrNull<SaveService>("/root/SaveService");
            saveService?.EnsureSlotsExist();

            var sceneRouter = GetNodeOrNull<SceneRouter>("/root/SceneRouter");
            if (sceneRouter is not null)
            {
                sceneRouter.GoToMainMenu();
                return;
            }

            var directErr = GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
            if (directErr != Error.Ok)
            {
                GD.PushError($"Boot fallback fallito: {directErr}");
            }
        }
        catch (Exception ex)
        {
            GD.PushError($"Errore in boot: {ex}");
            var err = GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
            if (err != Error.Ok)
            {
                GD.PushError($"Cambio scena di emergenza fallito: {err}");
            }
        }
    }
}
