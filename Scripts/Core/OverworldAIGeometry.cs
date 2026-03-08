using System;

public static partial class OverworldAI
{
    public static bool HasLineOfSight(int[,] grid, float startX, float startY, float endX, float endY)
    {
        var x0 = (int)MathF.Floor(startX);
        var y0 = (int)MathF.Floor(startY);
        var x1 = (int)MathF.Floor(endX);
        var y1 = (int)MathF.Floor(endY);

        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;
        var width = grid.GetLength(1);
        var height = grid.GetLength(0);

        while (true)
        {
            if (x0 < 0 || y0 < 0 || x0 >= width || y0 >= height || grid[y0, x0] == (int)TileType.Wall)
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
