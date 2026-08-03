public sealed class PersistResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public SceneRoute Route { get; init; } = SceneRoute.None;
    public bool Recoverable { get; init; }
    public bool PersistenceChanged { get; init; }

    public static PersistResult Succeeded(SceneRoute route, bool persistenceChanged = true, string message = "")
    {
        return new PersistResult
        {
            Success = true,
            Route = route,
            Message = message,
            PersistenceChanged = persistenceChanged,
        };
    }

    public static PersistResult Failed(SceneRoute route, string message, bool recoverable = true, bool persistenceChanged = false)
    {
        return new PersistResult
        {
            Success = false,
            Route = route,
            Message = message,
            Recoverable = recoverable,
            PersistenceChanged = persistenceChanged,
        };
    }
}

public interface ISaveFlowPersistence
{
    bool SaveToSlot(int slotId);
    PersistResult PurgeRunSnapshots(int proceduralSeed);
}
