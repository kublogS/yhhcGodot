using Godot;

public partial class ExploreController
{
    private void WirePauseOverlayActions()
    {
        _pause.ResumeRequested += () => ApplyInteractionInputLock();
        _pause.ReturnToIndexRequested += HandlePauseReturnToIndex;
        _pause.MainMenuRequested += HandlePauseMainMenu;
    }

    private void HandlePauseReturnToIndex()
    {
        var session = GameSession.Instance;
        var seed = session.ProcSeed;
        if (seed > 0)
        {
            SaveService.Instance.PurgeRunSnapshots(seed);
        }

        _pause.SetOverlayVisible(false);
        BuildSpawnHub();
        _hud.ShowTransientStatus("Run procedural resettata");
    }

    private void HandlePauseMainMenu()
    {
        var session = GameSession.Instance;
        if (session.ProcSeed > 0)
        {
            SaveService.Instance.PurgeRunSnapshots(session.ProcSeed);
        }

        _pause.SetOverlayVisible(false);
        SceneRouter.Instance.GoToMainMenu();
    }
}
