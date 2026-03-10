using System;
using System.Collections.Generic;
using Godot;

public partial class SaveService
{
    private static SavedRuntime BuildRuntimePayload(GameSession session)
    {
        var inOverworld = IsInActiveOverworld(session);
        var world = new SavedWorld();
        if (inOverworld)
        {
            var dungeon = session.CurrentDungeon!;
            world.Width = dungeon.Width;
            world.Height = dungeon.Height;
            world.GridFlat = FlattenGrid(dungeon.Grid);
            world.Seed = dungeon.Seed > 0 ? dungeon.Seed : session.ProcSeed;
            world.FloorIndex = dungeon.FloorIndex;
            world.PlayerX = session.PlayerWorldPosition.X;
            world.PlayerY = session.PlayerWorldPosition.Y;
            world.PlayerZ = session.PlayerWorldPosition.Z;
            world.PlayerYaw = session.PlayerYawRadians;
        }

        return new SavedRuntime
        {
            World = world,
            OverworldEnemies = inOverworld ? new List<OverworldEnemyModel>(session.OverworldEnemies) : new List<OverworldEnemyModel>(),
            LastEncounterContext = session.LastEncounterContext,
            DeepestFloor = session.DeepestFloor,
            LabyrinthCompletions = session.LabyrinthCompletions,
            HasEnteredOverworld = inOverworld,
            RunActive = inOverworld,
            ProceduralSeed = inOverworld ? (session.ProcSeed > 0 ? session.ProcSeed : world.Seed) : 0,
            ProceduralFloor = inOverworld ? Math.Max(0, session.ProcFloor) : 0,
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
        session.ProcMaxFloors = Math.Max(1, payload.Runtime.ProceduralMaxFloors <= 0 ? 20 : payload.Runtime.ProceduralMaxFloors);

        var loadedDungeon = RuntimeToDungeon(payload.Runtime.World);
        var hasOverworldSnapshot = payload.Runtime.RunActive
                                   && payload.Runtime.HasEnteredOverworld
                                   && loadedDungeon is not null;
        if (!hasOverworldSnapshot)
        {
            session.ReturnToSpawnHub();
            return;
        }

        session.CurrentDungeon = loadedDungeon;
        session.OverworldEnemies = payload.Runtime.OverworldEnemies;
        session.PlayerWorldPosition = new Vector3(payload.Runtime.World.PlayerX, payload.Runtime.World.PlayerY, payload.Runtime.World.PlayerZ);
        session.PlayerYawRadians = payload.Runtime.World.PlayerYaw;
        session.HasEnteredOverworld = true;
        session.RunActive = true;
        session.ProcSeed = payload.Runtime.ProceduralSeed > 0 ? payload.Runtime.ProceduralSeed : payload.Runtime.World.Seed;
        session.ProcFloor = Math.Max(0, payload.Runtime.ProceduralFloor);
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

        var roomIdGrid = new int[world.Height, world.Width];
        for (var y = 0; y < world.Height; y++)
        {
            for (var x = 0; x < world.Width; x++)
            {
                roomIdGrid[y, x] = -1;
            }
        }

        var dungeon = new DungeonData
        {
            Grid = grid,
            RoomIdGrid = roomIdGrid,
            PlayerSpawn = new Vector3(world.PlayerX, world.PlayerY, world.PlayerZ),
            Seed = world.Seed,
            FloorIndex = Math.Max(0, world.FloorIndex),
            LayoutTuned = true,
        };
        HydrateRuntimeSets(dungeon);
        DungeonLayoutTuner.EnsureWallEnvelope(dungeon);
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

    private static bool IsInActiveOverworld(GameSession session)
    {
        return session.RunActive
               && session.HasEnteredOverworld
               && session.CurrentDungeon is not null;
    }

    private static void HydrateRuntimeSets(DungeonData dungeon)
    {
        dungeon.BreakableTiles.Clear();
        dungeon.ExitTiles.Clear();
        dungeon.SaveTiles.Clear();
        for (var y = 0; y < dungeon.Height; y++)
        {
            for (var x = 0; x < dungeon.Width; x++)
            {
                var tile = (TileType)dungeon.Grid[y, x];
                if (tile == TileType.Breakable)
                {
                    dungeon.BreakableTiles.Add(new Vector2I(x, y));
                }
                else if (tile == TileType.Exit)
                {
                    var exit = new Vector2I(x, y);
                    dungeon.ExitTiles.Add(exit);
                    dungeon.ExitPosition = DungeonGenerator.GridToWorld(exit.X, exit.Y, DungeonBuilder.TileSize);
                }
                else if (tile == TileType.Save)
                {
                    dungeon.SaveTiles.Add(new Vector2I(x, y));
                }
            }
        }
    }
}
