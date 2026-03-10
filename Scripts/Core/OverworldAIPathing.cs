using System;
using System.Collections.Generic;

public static partial class OverworldAI
{
    private static (int X, int Y) GreedyStep(int[,] grid, int sx, int sy, int tx, int ty)
    {
        var options = new List<(int X, int Y)>();
        var dx = tx - sx;
        var dy = ty - sy;
        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            options.Add((Math.Sign(dx), 0));
            options.Add((0, Math.Sign(dy)));
        }
        else
        {
            options.Add((0, Math.Sign(dy)));
            options.Add((Math.Sign(dx), 0));
        }

        options.AddRange(new[] { (1, 0), (-1, 0), (0, 1), (0, -1) });
        foreach (var (ox, oy) in options)
        {
            if (ox == 0 && oy == 0)
            {
                continue;
            }

            var nx = sx + ox;
            var ny = sy + oy;
            if (IsWalkable(grid, nx, ny))
            {
                return (ox, oy);
            }
        }

        return (0, 0);
    }

    private static (int X, int Y) BfsNextStep(int[,] grid, int sx, int sy, int tx, int ty)
    {
        if (!IsWalkable(grid, tx, ty))
        {
            return (0, 0);
        }

        var prev = new Dictionary<(int, int), (int, int)?> { [(sx, sy)] = null };
        var queue = new Queue<(int, int)>();
        queue.Enqueue((sx, sy));

        var found = false;
        while (queue.Count > 0 && prev.Count < 1500)
        {
            var node = queue.Dequeue();
            if (node == (tx, ty))
            {
                found = true;
                break;
            }

            foreach (var step in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
            {
                var next = (node.Item1 + step.Item1, node.Item2 + step.Item2);
                if (prev.ContainsKey(next) || !IsWalkable(grid, next.Item1, next.Item2))
                {
                    continue;
                }

                prev[next] = node;
                queue.Enqueue(next);
            }
        }

        if (!found)
        {
            return (0, 0);
        }

        var current = (tx, ty);
        while (prev[current] is not null && prev[current] != (sx, sy))
        {
            current = prev[current]!.Value;
        }

        return (Math.Sign(current.Item1 - sx), Math.Sign(current.Item2 - sy));
    }

    private static bool IsWalkable(int[,] grid, int x, int y)
    {
        var width = grid.GetLength(1);
        var height = grid.GetLength(0);
        if (x < 0 || y < 0 || x >= width || y >= height)
        {
            return false;
        }

        var tile = (TileType)grid[y, x];
        return tile == TileType.Floor
               || tile == TileType.Doorway
               || tile == TileType.Threshold
               || tile == TileType.Exit
               || tile == TileType.Breakable
               || tile == TileType.Save;
    }
}
