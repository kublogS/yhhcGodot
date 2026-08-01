public sealed class RewardFinishResult
{
    public SceneRoute Route { get; init; } = SceneRoute.None;
    public bool PersistFailed { get; init; }
    public string Message { get; init; } = string.Empty;
}
