using System;
using System.Collections.Generic;
using Godot;

public partial class GameSession : Node, INewGameSessionContext, IRewardSessionContext, IBattleSessionContext
{
    public static GameSession Instance { get; private set; } = null!;

    public GameState? State { get; set; }
    public int CurrentSlot { get; set; } = 1;
    public DungeonData? CurrentDungeon { get; set; }
    public List<OverworldEnemyModel> OverworldEnemies { get; set; } = new();
    public Vector3 PlayerWorldPosition { get; set; } = Vector3.Zero;
    public float PlayerYawRadians { get; set; }
    public string LastEncounterContext { get; set; } = string.Empty;
    public List<string> LastBattleLog { get; } = new();
    public bool HasPendingMoveReplace { get; set; }
    public MoveModel? PendingMove { get; set; }
    public int PendingReplaceIndex { get; set; }
    public int DeepestFloor { get; set; }
    public int LabyrinthCompletions { get; set; }
    public RuntimeRunState CurrentRun { get; } = new();
    public IBattleAnalyticsSink Analytics { get; set; } = BattleAnalyticsLogSink.Instance;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        InputSetup.EnsureActions();
    }

    public void CreateNewGame(string playerName)
    {
        State = GameState.New(playerName, (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        DeepestFloor = 1;
        LabyrinthCompletions = 0;
        ClearEncounterWorldEnemies();
        CurrentRun.Reset(CurrentRun.MaxFloors);
        CurrentDungeon = null;
        OverworldEnemies.Clear();
        PlayerWorldPosition = Vector3.Zero;
        PlayerYawRadians = 0f;
    }

    public void StartEncounterWithEnemy(OverworldEnemyModel enemy, float reinforcementRadius = 5f)
    {
        if (State is null)
        {
            return;
        }

        var plan = OverworldEncounterPlanner.Build(enemy, OverworldEnemies, State.Rng, reinforcementRadius);
        CaptureEncounterWorldEnemies(plan.ConsumedEnemies);
        StartEncounterWithEnemies(plan.ActiveEnemies, plan.QueuedEnemies, "overworld");
        foreach (var consumed in plan.ConsumedEnemies)
        {
            OverworldEnemies.Remove(consumed);
        }
    }

    public void StartEncounterWithEnemies(List<CharacterModel> enemies, List<CharacterModel>? queued = null, string context = "")
    {
        if (State is null || enemies.Count == 0)
        {
            return;
        }

        BattleStateService.BeginEncounter(State, enemies, queued);
        LastEncounterContext = context;
        LastBattleLog.Clear();
        Analytics.BeginBattle(State, context);
    }

    public string AddReinforcement(CharacterModel enemy)
    {
        if (State is null)
        {
            return "queue";
        }

        return BattleStateService.AddReinforcement(State, enemy);
    }
}
