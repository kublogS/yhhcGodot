using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class GameSession : Node
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
    public bool HasEnteredOverworld { get; set; }
    public bool RunActive { get; set; }
    public int ProcSeed { get; set; }
    public int ProcFloor { get; set; }
    public int ProcMaxFloors { get; set; } = 20;

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
        HasEnteredOverworld = false;
        RunActive = false;
        ProcSeed = 0;
        ProcFloor = 0;
        CurrentDungeon = null;
        OverworldEnemies.Clear();
        PlayerWorldPosition = Vector3.Zero;
        PlayerYawRadians = 0f;
    }

    public void StartEncounterWithEnemy(OverworldEnemyModel enemy, float reinforcementRadius = 10f)
    {
        if (State is null)
        {
            return;
        }

        var nearby = OverworldEnemies
            .Where(e => e.Active && e != enemy)
            .Select(e => (Enemy: e, Distance: e.XYDistanceTo(enemy.X, enemy.Y)))
            .Where(e => e.Distance <= reinforcementRadius)
            .OrderBy(e => e.Distance)
            .ToList();

        var active = new List<CharacterModel> { enemy.ToBattleCharacter(State.Rng) };
        var queue = new List<CharacterModel>();
        foreach (var candidate in nearby)
        {
            if (active.Count < 4)
            {
                active.Add(candidate.Enemy.ToBattleCharacter(State.Rng));
            }
            else
            {
                queue.Add(candidate.Enemy.ToBattleCharacter(State.Rng));
            }
        }

        StartEncounterWithEnemies(active, queue, "overworld");
        OverworldEnemies.Remove(enemy);
        foreach (var item in nearby)
        {
            OverworldEnemies.Remove(item.Enemy);
        }
    }

    public void StartEncounterWithEnemies(List<CharacterModel> enemies, List<CharacterModel>? queued = null, string context = "")
    {
        if (State is null || enemies.Count == 0)
        {
            return;
        }

        State.ResetBattleInstance();
        State.SetBattleGroups(enemies, queued);
        LastEncounterContext = context;
        LastBattleLog.Clear();
    }

    public string AddReinforcement(CharacterModel enemy)
    {
        if (State is null)
        {
            return "queue";
        }

        var before = State.Enemies.Count;
        State.AddEnemyToBattle(enemy);
        return State.Enemies.Count > before ? "active" : "queue";
    }
}

public static class OverworldEnemyExtensions
{
    public static float XYDistanceTo(this OverworldEnemyModel enemy, float x, float y)
    {
        return MathF.Sqrt((enemy.X - x) * (enemy.X - x) + (enemy.Y - y) * (enemy.Y - y));
    }
}
