using System;
using System.Collections.Generic;
using Godot;

public partial class SaveService
{
    private static SavedRuntime BuildRuntimePayload(GameSession session)
    {
        var world = new SavedWorld();
        if (session.CurrentDungeon is not null)
        {
            world.Width = session.CurrentDungeon.Width;
            world.Height = session.CurrentDungeon.Height;
            world.GridFlat = FlattenGrid(session.CurrentDungeon.Grid);
            world.PlayerX = session.PlayerWorldPosition.X;
            world.PlayerY = session.PlayerWorldPosition.Y;
            world.PlayerZ = session.PlayerWorldPosition.Z;
            world.PlayerYaw = session.PlayerYawRadians;
        }

        return new SavedRuntime
        {
            World = world,
            OverworldEnemies = session.OverworldEnemies,
            LastEncounterContext = session.LastEncounterContext,
            DeepestFloor = session.DeepestFloor,
            LabyrinthCompletions = session.LabyrinthCompletions,
            HasEnteredOverworld = session.HasEnteredOverworld,
            RunActive = session.RunActive,
            ProceduralSeed = session.ProcSeed,
            ProceduralFloor = session.ProcFloor,
            ProceduralMaxFloors = session.ProcMaxFloors,
        };
    }

    private static void ApplyLoadedPayload(GameSession session, SaveFileData payload, int slotId)
    {
        session.State = payload.GameState;
        EnsurePlayerMoves(session.State!);
        session.State!.SyncEnemyLegacy();
        session.State.Rng.State = payload.Rng.RngState == 0 ? session.State.Rng.State : payload.Rng.RngState;
        session.CurrentSlot = slotId;
        session.DeepestFloor = payload.Runtime.DeepestFloor;
        session.LabyrinthCompletions = payload.Runtime.LabyrinthCompletions;
        session.LastEncounterContext = payload.Runtime.LastEncounterContext;
        session.OverworldEnemies = payload.Runtime.OverworldEnemies;
        session.CurrentDungeon = RuntimeToDungeon(payload.Runtime.World);
        session.PlayerWorldPosition = new Vector3(payload.Runtime.World.PlayerX, payload.Runtime.World.PlayerY, payload.Runtime.World.PlayerZ);
        session.PlayerYawRadians = payload.Runtime.World.PlayerYaw;
        session.HasEnteredOverworld = payload.Runtime.HasEnteredOverworld || session.CurrentDungeon is not null;
        session.RunActive = payload.Runtime.RunActive;
        session.ProcSeed = payload.Runtime.ProceduralSeed;
        session.ProcFloor = payload.Runtime.ProceduralFloor;
        session.ProcMaxFloors = Math.Max(1, payload.Runtime.ProceduralMaxFloors <= 0 ? 20 : payload.Runtime.ProceduralMaxFloors);
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

    private static DungeonData? RuntimeToDungeon(SavedWorld world)
    {
        if (world.Width <= 0 || world.Height <= 0 || world.GridFlat.Count != world.Width * world.Height)
        {
            return null;
        }

        var grid = new int[world.Height, world.Width];
        var index = 0;
        for (var y = 0; y < world.Height; y++)
        {
            for (var x = 0; x < world.Width; x++)
            {
                grid[y, x] = world.GridFlat[index++];
            }
        }

        var dungeon = new DungeonData
        {
            Grid = grid,
            PlayerSpawn = new Vector3(world.PlayerX, world.PlayerY, world.PlayerZ),
        };
        DungeonLayoutTuner.EnsureComfortablePassages(dungeon);
        return dungeon;
    }

    private static List<int> FlattenGrid(int[,] grid)
    {
        var list = new List<int>(grid.Length);
        for (var y = 0; y < grid.GetLength(0); y++)
        {
            for (var x = 0; x < grid.GetLength(1); x++)
            {
                list.Add(grid[y, x]);
            }
        }

        return list;
    }
}
