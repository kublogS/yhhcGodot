using System;

public static partial class DungeonGenerator
{
    private const int CorridorHalfWidth = 2;
    private const int DoorHalfWidth = 2;

    private static void CarveCorridor(int[,] grid, int x1, int y1, int x2, int y2)
    {
        var x = x1;
        var y = y1;
        while (x != x2)
        {
            CarveCorridorBand(grid, x, y, horizontal: true);
            x += Math.Sign(x2 - x);
        }

        while (y != y2)
        {
            CarveCorridorBand(grid, x, y, horizontal: false);
            y += Math.Sign(y2 - y);
        }

        CarveCorridorBand(grid, x, y, horizontal: true);
        CarveCorridorBand(grid, x, y, horizontal: false);
    }

    private static void CarveDoorway(int[,] grid, int x, int centerY)
    {
        for (var offset = -DoorHalfWidth; offset <= DoorHalfWidth; offset++)
        {
            var y = centerY + offset;
            if (InBounds(grid, x, y))
            {
                grid[y, x] = (int)TileType.Door;
            }
        }
    }

    private static void CarveCorridorBand(int[,] grid, int x, int y, bool horizontal)
    {
        if (horizontal)
        {
            for (var offset = -CorridorHalfWidth; offset <= CorridorHalfWidth; offset++)
            {
                var ny = y + offset;
                if (InBounds(grid, x, ny))
                {
                    grid[ny, x] = (int)TileType.Floor;
                }
            }

            return;
        }

        for (var offset = -CorridorHalfWidth; offset <= CorridorHalfWidth; offset++)
        {
            var nx = x + offset;
            if (InBounds(grid, nx, y))
            {
                grid[y, nx] = (int)TileType.Floor;
            }
        }
    }

    private static bool InBounds(int[,] grid, int x, int y)
    {
        return x >= 0 && y >= 0 && x < grid.GetLength(1) && y < grid.GetLength(0);
    }
}
