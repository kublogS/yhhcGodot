using System.Collections.Generic;
using Godot;

public sealed partial class ProceduralTilemapBuilder
{
    private static readonly Vector2I[] Neighbor8 =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        new(1, 1), new(-1, -1), new(1, -1), new(-1, 1),
    };

    private void FinalizeTopology()
    {
        ConvertDoorwaysToThresholds();
        EncloseWalkableZonesWithWalls();
        EnsureMapOuterFrameWalls();
    }

    private void ConvertDoorwaysToThresholds()
    {
        for (var y = 0; y < _gridHeight; y++)
        {
            for (var x = 0; x < _gridWidth; x++)
            {
                if ((TileType)_grid[y, x] == TileType.Doorway)
                {
                    _grid[y, x] = (int)TileType.Threshold;
                }
            }
        }
    }

    private void EncloseWalkableZonesWithWalls()
    {
        var toWalls = new List<Vector2I>();
        for (var y = 1; y < _gridHeight - 1; y++)
        {
            for (var x = 1; x < _gridWidth - 1; x++)
            {
                if (!IsWalkableTile((TileType)_grid[y, x]))
                {
                    continue;
                }

                foreach (var dir in Neighbor8)
                {
                    var nx = x + dir.X;
                    var ny = y + dir.Y;
                    if ((TileType)_grid[ny, nx] == TileType.Void)
                    {
                        toWalls.Add(new Vector2I(nx, ny));
                    }
                }
            }
        }

        foreach (var pos in toWalls)
        {
            _grid[pos.Y, pos.X] = (int)TileType.Wall;
        }
    }

    private void EnsureMapOuterFrameWalls()
    {
        for (var x = 0; x < _gridWidth; x++)
        {
            if ((TileType)_grid[0, x] == TileType.Void) _grid[0, x] = (int)TileType.Wall;
            if ((TileType)_grid[_gridHeight - 1, x] == TileType.Void) _grid[_gridHeight - 1, x] = (int)TileType.Wall;
        }

        for (var y = 0; y < _gridHeight; y++)
        {
            if ((TileType)_grid[y, 0] == TileType.Void) _grid[y, 0] = (int)TileType.Wall;
            if ((TileType)_grid[y, _gridWidth - 1] == TileType.Void) _grid[y, _gridWidth - 1] = (int)TileType.Wall;
        }
    }

    private static bool IsWalkableTile(TileType tile)
    {
        return tile is TileType.Floor or TileType.Doorway or TileType.Threshold or TileType.Exit or TileType.Breakable or TileType.Save;
    }
}
