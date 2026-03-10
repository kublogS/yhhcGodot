using System;

public static class RoomShapeMutator
{
    public static int[,] BuildTiles(int width, int height, Random rng, RoomShapeProfile profile)
    {
        var tiles = new int[height, width];
        FillBaseRoom(tiles, width, height);
        ApplyInteriorNiches(tiles, width, height, rng, profile.NicheCount);

        if (profile.UseCornerChamfer)
        {
            ApplyCornerChamfers(tiles, width, height);
        }

        ApplyBoundaryStyle(tiles, width, height, rng, profile.NorthStyle, isHorizontal: true, start: true);
        ApplyBoundaryStyle(tiles, width, height, rng, profile.SouthStyle, isHorizontal: true, start: false);
        ApplyBoundaryStyle(tiles, width, height, rng, profile.WestStyle, isHorizontal: false, start: true);
        ApplyBoundaryStyle(tiles, width, height, rng, profile.EastStyle, isHorizontal: false, start: false);

        for (var i = 0; i < profile.JaggedPasses; i++)
        {
            ApplyJaggedAccent(tiles, width, height, rng);
        }

        EnsureOuterFrame(tiles, width, height);
        return tiles;
    }

    private static void FillBaseRoom(int[,] tiles, int width, int height)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                tiles[y, x] = (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                    ? (int)TileType.Wall
                    : (int)TileType.Floor;
            }
        }
    }

    private static void ApplyInteriorNiches(int[,] tiles, int width, int height, Random rng, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var side = rng.Next(4);
            var nicheWidth = rng.Next(2, 5);
            var nicheDepth = rng.Next(1, 3);
            if (side is 0 or 1)
            {
                var sx = rng.Next(2, Math.Max(3, width - nicheWidth - 1));
                var sy = side == 0 ? 1 : Math.Max(1, height - nicheDepth - 1);
                for (var y = sy; y < sy + nicheDepth; y++)
                {
                    for (var x = sx; x < sx + nicheWidth; x++)
                    {
                        tiles[y, x] = (int)TileType.Wall;
                    }
                }
            }
            else
            {
                var sy = rng.Next(2, Math.Max(3, height - nicheWidth - 1));
                var sx = side == 2 ? 1 : Math.Max(1, width - nicheDepth - 1);
                for (var y = sy; y < sy + nicheWidth; y++)
                {
                    for (var x = sx; x < sx + nicheDepth; x++)
                    {
                        tiles[y, x] = (int)TileType.Wall;
                    }
                }
            }
        }
    }

    private static void ApplyCornerChamfers(int[,] tiles, int width, int height)
    {
        CarveCorner(tiles, 1, 1);
        CarveCorner(tiles, width - 2, 1);
        CarveCorner(tiles, 1, height - 2);
        CarveCorner(tiles, width - 2, height - 2);
    }

    private static void CarveCorner(int[,] tiles, int x, int y)
    {
        tiles[y, x] = (int)TileType.Floor;
    }

    private static void ApplyBoundaryStyle(
        int[,] tiles,
        int width,
        int height,
        Random rng,
        BoundaryEdgeStyle style,
        bool isHorizontal,
        bool start)
    {
        if (style == BoundaryEdgeStyle.Orthogonal)
        {
            return;
        }

        var lane = isHorizontal ? (start ? 1 : height - 2) : (start ? 1 : width - 2);
        var from = 2;
        var to = (isHorizontal ? width : height) - 3;

        for (var i = from; i <= to; i++)
        {
            var roll = rng.NextDouble();
            if (style == BoundaryEdgeStyle.Chamfered && roll < 0.12)
            {
                SetWallInset(tiles, isHorizontal, lane, i, start, 1);
            }
            else if (style == BoundaryEdgeStyle.Jagged && roll < 0.2)
            {
                SetWallInset(tiles, isHorizontal, lane, i, start, rng.Next(1, 3));
            }
            else if (style == BoundaryEdgeStyle.FauxCurve && roll < 0.18)
            {
                SetWallInset(tiles, isHorizontal, lane, i, start, 1);
                if (i + 1 <= to)
                {
                    SetWallInset(tiles, isHorizontal, lane, i + 1, start, 1);
                }
            }
        }
    }

    private static void SetWallInset(int[,] tiles, bool horizontal, int lane, int index, bool start, int depth)
    {
        var step = start ? 1 : -1;
        if (horizontal)
        {
            for (var d = 0; d < depth; d++)
            {
                var y = lane + ((d + 1) * step);
                if (y > 1 && y < tiles.GetLength(0) - 1)
                {
                    tiles[y, index] = (int)TileType.Wall;
                }
            }

            return;
        }

        for (var d = 0; d < depth; d++)
        {
            var x = lane + ((d + 1) * step);
            if (x > 1 && x < tiles.GetLength(1) - 1)
            {
                tiles[index, x] = (int)TileType.Wall;
            }
        }
    }

    private static void ApplyJaggedAccent(int[,] tiles, int width, int height, Random rng)
    {
        var cx = rng.Next(3, width - 3);
        var cy = rng.Next(3, height - 3);
        tiles[cy, cx] = (int)TileType.Wall;
        if (rng.NextDouble() < 0.5) tiles[cy + 1, cx] = (int)TileType.Wall;
        if (rng.NextDouble() < 0.5) tiles[cy, cx + 1] = (int)TileType.Wall;
    }

    private static void EnsureOuterFrame(int[,] tiles, int width, int height)
    {
        for (var x = 0; x < width; x++)
        {
            tiles[0, x] = (int)TileType.Wall;
            tiles[height - 1, x] = (int)TileType.Wall;
        }

        for (var y = 0; y < height; y++)
        {
            tiles[y, 0] = (int)TileType.Wall;
            tiles[y, width - 1] = (int)TileType.Wall;
        }
    }
}
