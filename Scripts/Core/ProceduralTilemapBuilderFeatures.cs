using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public sealed partial class ProceduralTilemapBuilder
{
    private void AddCorridorBranches()
    {
        var corridorList = _corridorTiles.ToList();
        Shuffle(_rng, corridorList);
        var branches = 0;
        foreach (var tile in corridorList)
        {
            if (branches >= 5 || _rng.NextDouble() > 0.035)
            {
                continue;
            }

            var len = _rng.Next(1, 4);
            var dirs = new[] { new Vector2I(1, 0), new Vector2I(-1, 0), new Vector2I(0, 1), new Vector2I(0, -1) };
            var dir = dirs[_rng.Next(dirs.Length)];
            var pos = tile;
            for (var i = 0; i < len; i++)
            {
                pos += dir;
                if (!InBounds(pos, 1))
                {
                    break;
                }

                CarveFloor(pos);
            }

            branches++;
        }
    }

    private void AddSecretRooms()
    {
        foreach (var (roomId, bounds) in _roomBounds)
        {
            var type = _graph.Nodes[roomId].Type;
            if (type is ProcRoomType.Boss or ProcRoomType.Shop || _rng.NextDouble() > 0.12)
            {
                continue;
            }

            var side = _rng.Next(4) switch { 0 => 'N', 1 => 'S', 2 => 'E', _ => 'W' };
            var socket = bounds.Position + _doorSockets[roomId][side];
            var origin = side switch
            {
                'N' => new Vector2I(socket.X - 3, socket.Y - 6),
                'S' => new Vector2I(socket.X - 3, socket.Y + 2),
                'W' => new Vector2I(socket.X - 6, socket.Y - 3),
                _ => new Vector2I(socket.X + 2, socket.Y - 3),
            };
            if (!IsAreaEmpty(origin, 6, 6))
            {
                continue;
            }

            for (var y = origin.Y; y < origin.Y + 6; y++)
            {
                for (var x = origin.X; x < origin.X + 6; x++)
                {
                    var isBorder = x == origin.X || x == origin.X + 5 || y == origin.Y || y == origin.Y + 5;
                    _grid[y, x] = isBorder ? (int)TileType.Wall : (int)TileType.Floor;
                }
            }

            SetTile(socket, TileType.Breakable);
            _breakableTiles.Add(socket);
        }
    }

    private bool IsAreaEmpty(Vector2I origin, int width, int height)
    {
        if (origin.X < 1 || origin.Y < 1 || origin.X + width >= _gridWidth || origin.Y + height >= _gridHeight)
        {
            return false;
        }

        for (var y = origin.Y; y < origin.Y + height; y++)
        {
            for (var x = origin.X; x < origin.X + width; x++)
            {
                if (_grid[y, x] != (int)TileType.Void)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void AddExitTile()
    {
        if (!_roomBounds.TryGetValue(_graph.BossId, out var bossBounds))
        {
            return;
        }

        var exit = new Vector2I(bossBounds.Position.X + (bossBounds.Size.X / 2), bossBounds.Position.Y + (bossBounds.Size.Y / 2));
        SetTile(exit, TileType.Exit);
        _exitTiles.Add(exit);
    }

    private void SetTile(Vector2I pos, TileType tile)
    {
        if (InBounds(pos))
        {
            _grid[pos.Y, pos.X] = (int)tile;
        }
    }

    private void CarveFloor(Vector2I pos)
    {
        if (!InBounds(pos, 1))
        {
            return;
        }

        if ((TileType)_grid[pos.Y, pos.X] == TileType.Exit)
        {
            return;
        }

        _grid[pos.Y, pos.X] = (int)TileType.Floor;
        _corridorTiles.Add(pos);
    }

    private Vector2I ClampInside(Vector2I pos)
    {
        return new Vector2I(Math.Clamp(pos.X, 1, _gridWidth - 2), Math.Clamp(pos.Y, 1, _gridHeight - 2));
    }

    private bool InBounds(Vector2I pos, int border = 0)
    {
        return pos.X >= border && pos.Y >= border && pos.X < _gridWidth - border && pos.Y < _gridHeight - border;
    }

    private static char Opposite(char dir)
    {
        return dir switch
        {
            'N' => 'S',
            'S' => 'N',
            'E' => 'W',
            _ => 'E',
        };
    }

    private static void Shuffle<T>(Random rng, IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
