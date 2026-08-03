using System.Collections.Generic;
using System.Text.Json;
using Godot;

public sealed class GameSessionCheckpoint
{
    private GameSessionCheckpoint() { }

    public GameState? State { get; private init; }
    public int CurrentSlot { get; private init; }
    public DungeonData? CurrentDungeon { get; private init; }
    public List<OverworldEnemyModel> OverworldEnemies { get; private init; } = new();
    public Vector3 PlayerWorldPosition { get; private init; }
    public float PlayerYawRadians { get; private init; }
    public string LastEncounterContext { get; private init; } = string.Empty;
    public List<string> LastBattleLog { get; private init; } = new();
    public bool HasPendingMoveReplace { get; private init; }
    public MoveModel? PendingMove { get; private init; }
    public int PendingReplaceIndex { get; private init; }
    public int DeepestFloor { get; private init; }
    public int LabyrinthCompletions { get; private init; }
    public RuntimeRunState CurrentRun { get; private init; } = new();

    public static GameSessionCheckpoint Capture(ISessionSnapshotHost session, bool cloneState)
    {
        return new GameSessionCheckpoint
        {
            State = cloneState ? CloneState(session.State) : session.State,
            CurrentSlot = session.CurrentSlot,
            CurrentDungeon = session.CurrentDungeon,
            OverworldEnemies = new List<OverworldEnemyModel>(session.OverworldEnemies),
            PlayerWorldPosition = session.PlayerWorldPosition,
            PlayerYawRadians = session.PlayerYawRadians,
            LastEncounterContext = session.LastEncounterContext,
            LastBattleLog = new List<string>(session.LastBattleLog),
            HasPendingMoveReplace = session.HasPendingMoveReplace,
            PendingMove = session.PendingMove,
            PendingReplaceIndex = session.PendingReplaceIndex,
            DeepestFloor = session.DeepestFloor,
            LabyrinthCompletions = session.LabyrinthCompletions,
            CurrentRun = RuntimeRunState.From(session.CurrentRun.ToContract()),
        };
    }

    public void Restore(ISessionSnapshotHost session)
    {
        session.State = State;
        session.CurrentSlot = CurrentSlot;
        session.CurrentDungeon = CurrentDungeon;
        session.OverworldEnemies = new List<OverworldEnemyModel>(OverworldEnemies);
        session.PlayerWorldPosition = PlayerWorldPosition;
        session.PlayerYawRadians = PlayerYawRadians;
        session.LastEncounterContext = LastEncounterContext;
        session.LastBattleLog.Clear();
        session.LastBattleLog.AddRange(LastBattleLog);
        session.HasPendingMoveReplace = HasPendingMoveReplace;
        session.PendingMove = PendingMove;
        session.PendingReplaceIndex = PendingReplaceIndex;
        session.DeepestFloor = DeepestFloor;
        session.LabyrinthCompletions = LabyrinthCompletions;
        session.CurrentRun.Apply(CurrentRun.ToContract());
    }

    private static GameState? CloneState(GameState? state)
    {
        if (state is null)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(state, SaveServiceSettings.JsonOptions);
        return JsonSerializer.Deserialize<GameState>(json, SaveServiceSettings.JsonOptions);
    }
}
