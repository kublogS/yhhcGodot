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
            if (Database.Instance is not null)
            {
                Database.Instance.LoadAll();
            }

            if (SaveService.Instance is not null)
            {
                SaveService.Instance.EnsureSlotsExist();
            }

            CombatDebugTest.Run();

            if (SceneRouter.Instance is not null)
            {
                SceneRouter.Instance.GoToMainMenu();
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
