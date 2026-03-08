public sealed class NewGameFlowCoordinator
{
    private readonly GameSession _session;
    private readonly SaveService _saveService;

    public NewGameFlowCoordinator(GameSession session, SaveService saveService)
    {
        _session = session;
        _saveService = saveService;
    }

    public SceneRoute StartNewGame(string playerName)
    {
        _session.CreateNewGame(playerName.Trim());
        _saveService.SaveToSlot(_session.CurrentSlot);
        return SceneRoute.Explore;
    }
}
