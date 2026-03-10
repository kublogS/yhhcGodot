using System.Collections.Generic;
using Godot;

public static class DungeonLayoutTuner
{
    private static readonly Vector2I[] Neighbor8 =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        new(1, 1), new(-1, -1), new(1, -1), new(-1, 1),
    };

    public static void EnsureComfortablePassages(DungeonData dungeon)
    {
        var source = dungeon.Grid;
        if (source.GetLength(0) < 3 || source.GetLength(1) < 3)
        {
            return;
        }

        var tuned = (int[,])source.Clone();
        WidenDoors(source, tuned);
        WidenNarrowCorridors(source, tuned);
        dungeon.Grid = tuned;
        dungeon.PlayerSpawn = SnapToNearestWalkable(dungeon, dungeon.PlayerSpawn);
    }

    public static void EnsureWallEnvelope(DungeonData dungeon)
    {
        var grid = dungeon.Grid;
        if (grid.GetLength(0) < 3 || grid.GetLength(1) < 3)
        {
            return;
        }

        var toWalls = new List<Vector2I>();
        for (var y = 1; y < grid.GetLength(0) - 1; y++)
        {
            for (var x = 1; x < grid.GetLength(1) - 1; x++)
            {
                if (!IsWalkable(grid, x, y))
                {
                    continue;
                }

                foreach (var dir in Neighbor8)
                {
                    var nx = x + dir.X;
                    var ny = y + dir.Y;
                    if ((TileType)grid[ny, nx] == TileType.Void)
                    {
                        toWalls.Add(new Vector2I(nx, ny));
                    }
                }
            }
        }

        foreach (var pos in toWalls)
        {
            grid[pos.Y, pos.X] = (int)TileType.Wall;
        }

        for (var x = 0; x < grid.GetLength(1); x++)
        {
            if ((TileType)grid[0, x] == TileType.Void) grid[0, x] = (int)TileType.Wall;
            if ((TileType)grid[grid.GetLength(0) - 1, x] == TileType.Void) grid[grid.GetLength(0) - 1, x] = (int)TileType.Wall;
        }

        for (var y = 0; y < grid.GetLength(0); y++)
        {
            if ((TileType)grid[y, 0] == TileType.Void) grid[y, 0] = (int)TileType.Wall;
            if ((TileType)grid[y, grid.GetLength(1) - 1] == TileType.Void) grid[y, grid.GetLength(1) - 1] = (int)TileType.Wall;
        }
    }

    private static void WidenDoors(int[,] source, int[,] target)
    {
        for (var y = 1; y < source.GetLength(0) - 1; y++)
        {
            for (var x = 1; x < source.GetLength(1) - 1; x++)
            {
                var tile = (TileType)source[y, x];
                if (tile is not (TileType.Doorway or TileType.Threshold))
                {
                    continue;
                }

                Carve(target, x, y - 1);
                Carve(target, x, y + 1);
            }
        }
    }

    private static void WidenNarrowCorridors(int[,] source, int[,] target)
    {
        for (var y = 1; y < source.GetLength(0) - 1; y++)
        {
            for (var x = 1; x < source.GetLength(1) - 1; x++)
            {
                if (!IsWalkable(source, x, y))
                {
                    continue;
                }

                var verticalNarrow = IsWall(source, x - 1, y) && IsWall(source, x + 1, y) && IsWalkable(source, x, y - 1) && IsWalkable(source, x, y + 1);
                if (verticalNarrow)
                {
                    Carve(target, x - 1, y);
                    Carve(target, x + 1, y);
                }

                var horizontalNarrow = IsWall(source, x, y - 1) && IsWall(source, x, y + 1) && IsWalkable(source, x - 1, y) && IsWalkable(source, x + 1, y);
                if (horizontalNarrow)
                {
                    Carve(target, x, y - 1);
                    Carve(target, x, y + 1);
                }
            }
        }
    }

    private static Vector3 SnapToNearestWalkable(DungeonData dungeon, Vector3 world)
    {
        var grid = DungeonGenerator.WorldToGrid(world, DungeonBuilder.TileSize);
        if (dungeon.IsWalkable(grid.X, grid.Y))
        {
            return world;
        }

        for (var r = 1; r <= 4; r++)
        {
            for (var y = -r; y <= r; y++)
            {
                for (var x = -r; x <= r; x++)
                {
                    var gx = grid.X + x;
                    var gy = grid.Y + y;
                    if (!dungeon.IsWalkable(gx, gy))
                    {
                        continue;
                    }

                    var snapped = DungeonGenerator.GridToWorld(gx, gy, DungeonBuilder.TileSize);
                    return new Vector3(snapped.X, world.Y, snapped.Z);
                }
            }
        }

        return world;
    }

    private static bool IsWalkable(int[,] grid, int x, int y)
    {
        var tile = (TileType)grid[y, x];
        return tile is TileType.Floor or TileType.Doorway or TileType.Threshold or TileType.Exit or TileType.Breakable or TileType.Save;
    }

    private static bool IsWall(int[,] grid, int x, int y)
    {
        return (TileType)grid[y, x] == TileType.Wall;
    }

    private static void Carve(int[,] target, int x, int y)
    {
        if ((TileType)target[y, x] != TileType.Exit)
        {
            target[y, x] = (int)TileType.Floor;
        }
    }
}
