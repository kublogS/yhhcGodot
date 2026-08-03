using System;
using System.Collections.Generic;
using Godot;

public static class DungeonRuntimeSnapshotMapper
{
    public static SavedWorld ToSavedWorld(DungeonData dungeon, Vector3 playerWorldPosition, float playerYaw, int fallbackSeed)
    {
        return new SavedWorld
        {
            Width = dungeon.Width,
            Height = dungeon.Height,
            GridFlat = FlattenGrid(dungeon.Grid),
            BrokenBreakablePairs = SavedWorldBreakableCodec.Flatten(dungeon.BrokenBreakableTiles),
            Seed = dungeon.Seed > 0 ? dungeon.Seed : fallbackSeed,
            FloorIndex = dungeon.FloorIndex,
            PlayerX = playerWorldPosition.X,
            PlayerY = playerWorldPosition.Y,
            PlayerZ = playerWorldPosition.Z,
            PlayerYaw = playerYaw,
        };
    }

    public static DungeonData? ToDungeon(SavedWorld world)
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
        HydrateRuntimeSets(dungeon, world.BrokenBreakablePairs);
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

    private static void HydrateRuntimeSets(DungeonData dungeon, List<int>? brokenPairs = null)
    {
        dungeon.BreakableTiles.Clear();
        dungeon.BrokenBreakableTiles.Clear();
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
                    dungeon.ExitPosition = DungeonGenerator.GridToWorld(exit.X, exit.Y, DungeonMetrics.TileSize);
                }
                else if (tile == TileType.Save)
                {
                    dungeon.SaveTiles.Add(new Vector2I(x, y));
                }
            }
        }

        if (brokenPairs is null || brokenPairs.Count < 2)
        {
            return;
        }

        for (var i = 0; i + 1 < brokenPairs.Count; i += 2)
        {
            var x = brokenPairs[i];
            var y = brokenPairs[i + 1];
            if (x < 0 || y < 0 || x >= dungeon.Width || y >= dungeon.Height)
            {
                continue;
            }

            var tile = new Vector2I(x, y);
            dungeon.BrokenBreakableTiles.Add(tile);
            dungeon.BreakableTiles.Remove(tile);
            if (dungeon.Grid[y, x] == (int)TileType.Breakable)
            {
                dungeon.Grid[y, x] = (int)TileType.Floor;
            }
        }
    }
}
