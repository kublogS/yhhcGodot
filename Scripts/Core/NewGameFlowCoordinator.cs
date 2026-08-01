public sealed class NewGameFlowCoordinator
{
    private readonly INewGameSessionContext _session;
    private readonly ISaveFlowPersistence _persistence;

    public NewGameFlowCoordinator(INewGameSessionContext session, ISaveFlowPersistence persistence)
    {
        _session = session;
        _persistence = persistence;
    }

    public PersistResult StartNewGame(string playerName)
    {
        var checkpoint = GameSessionCheckpoint.Capture(_session, cloneState: false);
        _session.CreateNewGame(playerName.Trim());
        if (_persistence.SaveToSlot(_session.CurrentSlot))
        {
            return PersistResult.Succeeded(SceneRoute.Explore, message: "Partita creata.");
        }

        checkpoint.Restore(_session);
        return PersistResult.Failed(SceneRoute.NameEntry, "Salvataggio iniziale fallito. La nuova partita non è stata avviata.");
    }
}
