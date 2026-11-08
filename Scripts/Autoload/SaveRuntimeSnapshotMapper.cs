using System;
using System.Collections.Generic;
using Godot;

public static class SaveRuntimeSnapshotMapper
{
    public static SavedRuntime BuildRuntimePayload(GameSession session)
    {
        var runContract = session.CurrentRun.ToContract();
        var inOverworld = runContract.HasOverworldRuntime && session.CurrentDungeon is not null;
        var world = new SavedWorld();
        if (inOverworld)
        {
            world = DungeonRuntimeSnapshotMapper.ToSavedWorld(
                session.CurrentDungeon!,
                session.PlayerWorldPosition,
                session.PlayerYawRadians,
                session.CurrentRun.Seed);
        }

        var runtime = new SavedRuntime
        {
            World = world,
            OverworldEnemies = inOverworld ? new List<OverworldEnemyModel>(session.OverworldEnemies) : new List<OverworldEnemyModel>(),
            LastEncounterContext = session.LastEncounterContext,
            DeepestFloor = session.DeepestFloor,
            LabyrinthCompletions = session.LabyrinthCompletions,
        };
        ApplyRunState(runtime, runContract);
        return runtime;
    }

    public static void ApplyLoadedPayload(GameSession session, SaveFileData payload, int slotId)
    {
        var state = payload.GameState ?? throw new InvalidOperationException("Save privo di GameState.");
        session.State = state;
        EnsurePlayerMoves(state);
        state.Rng.State = payload.Rng.RngState == 0 ? state.Rng.State : payload.Rng.RngState;
        session.CurrentSlot = slotId;
        session.DeepestFloor = payload.Runtime.DeepestFloor;
        session.LabyrinthCompletions = payload.Runtime.LabyrinthCompletions;
        session.LastEncounterContext = payload.Runtime.LastEncounterContext;

        var loadedDungeon = DungeonRuntimeSnapshotMapper.ToDungeon(payload.Runtime.World);
        var contract = ResolveRunState(payload.Runtime);
        var playerPos = new Vector3(payload.Runtime.World.PlayerX, payload.Runtime.World.PlayerY, payload.Runtime.World.PlayerZ);
        session.ApplyRunContract(contract, loadedDungeon, payload.Runtime.OverworldEnemies, playerPos, payload.Runtime.World.PlayerYaw);
    }

    public static RunStateContract ResolveRunState(SavedRuntime runtime)
    {
        runtime.Run ??= new SavedRunState();
        var locationText = string.IsNullOrWhiteSpace(runtime.Run.Location) ? runtime.RunLocation : runtime.Run.Location;
        var fallbackOverworld = runtime.Run.Location.Equals(nameof(RunLocation.Overworld), StringComparison.OrdinalIgnoreCase) || runtime.HasEnteredOverworld;
        var contract = new RunStateContract
        {
            IsRunActive = runtime.Run.IsActive || runtime.RunActive,
            Location = RunStateContract.ParseLocation(locationText, fallbackOverworld),
            ProceduralSeed = ResolveSeed(runtime),
            ProceduralFloor = Math.Max(0, runtime.Run.Floor > 0 ? runtime.Run.Floor : runtime.ProceduralFloor),
            ProceduralMaxFloors = ResolveMaxFloors(runtime),
        };
        return contract.IsRunActive ? contract : RunStatePolicy.ReturnToIndice(contract.ProceduralMaxFloors);
    }

    public static void ApplyRunState(SavedRuntime runtime, RunStateContract contract)
    {
        runtime.Run = new SavedRunState
        {
            IsActive = contract.IsRunActive,
            Location = contract.Location.ToString(),
            Seed = contract.HasOverworldRuntime ? contract.ProceduralSeed : 0,
            Floor = contract.HasOverworldRuntime ? Math.Max(0, contract.ProceduralFloor) : 0,
            MaxFloors = contract.ProceduralMaxFloors <= 0 ? 20 : contract.ProceduralMaxFloors,
        };
        runtime.HasEnteredOverworld = contract.Location == RunLocation.Overworld;
        runtime.RunActive = contract.IsRunActive;
        runtime.RunLocation = runtime.Run.Location;
        runtime.ProceduralSeed = runtime.Run.Seed;
        runtime.ProceduralFloor = runtime.Run.Floor;
        runtime.ProceduralMaxFloors = runtime.Run.MaxFloors;
    }

    private static void EnsurePlayerMoves(GameState state)
    {
        while (state.Player.Moves.Count < 5)
        {
            state.Player.Moves.Add(null);
        }

        if (state.Player.Moves.Count > 0 && state.Player.Moves[0] is null)
        {
            state.Player.Moves[0] = Moves.BasicAttackMove();
        }
    }

    private static int ResolveSeed(SavedRuntime runtime)
    {
        if (runtime.Run.Seed > 0)
        {
            return runtime.Run.Seed;
        }

        if (runtime.ProceduralSeed > 0)
        {
            return runtime.ProceduralSeed;
        }

        return runtime.World.Seed;
    }

    private static int ResolveMaxFloors(SavedRuntime runtime)
    {
        if (runtime.Run.MaxFloors > 0)
        {
            return runtime.Run.MaxFloors;
        }

        return runtime.ProceduralMaxFloors <= 0 ? 20 : runtime.ProceduralMaxFloors;
    }
}
