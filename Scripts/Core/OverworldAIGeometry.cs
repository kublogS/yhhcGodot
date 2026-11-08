using System;
using Godot;

public static partial class OverworldAI
{
    public static bool HasLineOfSight(int[,] grid, float startX, float startY, float endX, float endY)
    {
        var start = DungeonMetrics.WorldToGrid(new Vector3(startX, 0f, startY), DungeonMetrics.TileSize);
        var end = DungeonMetrics.WorldToGrid(new Vector3(endX, 0f, endY), DungeonMetrics.TileSize);
        var x0 = start.X;
        var y0 = start.Y;
        var x1 = end.X;
        var y1 = end.Y;

        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;
        var width = grid.GetLength(1);
        var height = grid.GetLength(0);

        while (true)
        {
            if (x0 < 0 || y0 < 0 || x0 >= width || y0 >= height)
            {
                return false;
            }

            var tile = (TileType)grid[y0, x0];
            if (tile is TileType.Wall or TileType.Breakable)
            {
                return false;
            }

            if (x0 == x1 && y0 == y1)
            {
                return true;
            }

            var e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
}
