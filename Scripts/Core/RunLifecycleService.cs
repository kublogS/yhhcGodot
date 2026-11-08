using System;
using Godot;

public static class RunLifecycleService
{
    public static RunStateContract Capture(ISessionSnapshotHost session)
    {
        return session.CurrentRun.ToContract();
    }

    public static void ApplyLoadedRun(ISessionSnapshotHost session, RunStateContract contract, DungeonData? dungeon, List<OverworldEnemyModel>? enemies, Vector3 playerPosition, float playerYaw)
    {
        ApplyContract(session, contract);
        if (!contract.HasOverworldRuntime || dungeon is null)
        {
            ReturnToSpawnHub(session);
            return;
        }

        session.CurrentDungeon = dungeon;
        session.OverworldEnemies = enemies ?? new List<OverworldEnemyModel>();
        session.PlayerWorldPosition = playerPosition;
        session.PlayerYawRadians = playerYaw;
    }

    public static void BeginOverworldRun(ISessionSnapshotHost session)
    {
        if (session.State is null)
        {
            return;
        }

        var runContract = RunStatePolicy.EnterOverworld(
            session.State.Rng.NextInt(1, 9_999_999),
            proceduralFloor: 0,
            maxFloors: session.CurrentRun.MaxFloors);
        ApplyContract(session, runContract);
        GenerateCurrentFloor(session);
    }

    public static void GenerateNewDungeon(ISessionSnapshotHost session)
    {
        if (session.State is null)
        {
            return;
        }

        var seed = session.CurrentRun.Seed > 0 ? session.CurrentRun.Seed : session.State.Rng.NextInt(1, 9_999_999);
        var runContract = RunStatePolicy.EnterOverworld(seed, Math.Max(0, session.CurrentRun.Floor), session.CurrentRun.MaxFloors);
        ApplyContract(session, runContract);
        GenerateCurrentFloor(session);
    }

    public static bool TryAdvanceFloor(ISessionSnapshotHost session)
    {
        if (session.State is null || !session.CurrentRun.IsActive)
        {
            return false;
        }

        var next = RunStatePolicy.AdvanceFloor(Capture(session), out var runCompleted);
        if (runCompleted)
        {
            session.LabyrinthCompletions++;
            ReturnToSpawnHub(session);
            return false;
        }

        ApplyContract(session, next);
        GenerateCurrentFloor(session);
        return true;
    }

    public static void ReturnToSpawnHub(ISessionSnapshotHost session)
    {
        ApplyContract(session, RunStatePolicy.ReturnToIndice(session.CurrentRun.MaxFloors));
        session.CurrentDungeon = null;
        session.OverworldEnemies.Clear();
        session.PlayerWorldPosition = Vector3.Zero;
        session.PlayerYawRadians = 0f;
    }

    private static void ApplyContract(ISessionSnapshotHost session, RunStateContract contract)
    {
        session.CurrentRun.Apply(contract);
    }

    private static void GenerateCurrentFloor(ISessionSnapshotHost session)
    {
        if (session.State is null)
        {
            return;
        }

        session.CurrentDungeon = DungeonGenerator.Generate(session.CurrentRun.Seed, session.CurrentRun.Floor);
        session.PlayerWorldPosition = session.CurrentDungeon.PlayerSpawn + new Vector3(0f, 1.2f, 0f);
        session.PlayerYawRadians = 0f;
        session.OverworldEnemies = OverworldEnemySpawnFactory.BuildForDungeon(session.CurrentDungeon, session.CurrentRun.Seed);
        session.CurrentRun.Location = RunLocation.Overworld;
        session.DeepestFloor = Math.Max(session.DeepestFloor, session.CurrentRun.Floor + 1);
    }
}
