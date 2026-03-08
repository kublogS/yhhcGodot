using Godot;

public static class DungeonLayoutTuner
{
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

    private static void WidenDoors(int[,] source, int[,] target)
    {
        for (var y = 1; y < source.GetLength(0) - 1; y++)
        {
            for (var x = 1; x < source.GetLength(1) - 1; x++)
            {
                if ((TileType)source[y, x] != TileType.Door)
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
        return tile is TileType.Floor or TileType.Door or TileType.Exit or TileType.Breakable;
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
