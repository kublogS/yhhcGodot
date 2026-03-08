using System;
using System.Collections.Generic;
using Godot;

public sealed partial class ProceduralTilemapBuilder
{
    private enum CorridorStyle { L, Z, Drunk, Diag }

    private void ConnectRooms()
    {
        foreach (var (roomId, map) in _embed.Doors)
        {
            foreach (var (dir, neighborId) in map)
            {
                if (roomId > neighborId)
                {
                    continue;
                }

                RegisterNeighbor(roomId, neighborId);
                var roomRect = _roomBounds[roomId];
                var neighborRect = _roomBounds[neighborId];
                var a = roomRect.Position + _doorSockets[roomId][dir];
                var b = neighborRect.Position + _doorSockets[neighborId][Opposite(dir)];
                SetTile(a, TileType.Door);
                SetTile(b, TileType.Door);
                CarveCorridor(a, b);
            }
        }
    }

    private void RegisterNeighbor(int a, int b)
    {
        if (!_roomNeighbors.TryGetValue(a, out var aList))
        {
            aList = new List<int>();
            _roomNeighbors[a] = aList;
        }

        if (!_roomNeighbors.TryGetValue(b, out var bList))
        {
            bList = new List<int>();
            _roomNeighbors[b] = bList;
        }

        if (!aList.Contains(b)) aList.Add(b);
        if (!bList.Contains(a)) bList.Add(a);
    }

    private void CarveCorridor(Vector2I from, Vector2I to)
    {
        var style = ChooseCorridorStyle();
        var width = _rng.NextDouble() < 0.8 ? 1 : 2;
        switch (style)
        {
            case CorridorStyle.L:
                if (_rng.NextDouble() < 0.5)
                {
                    CarveLine(from, new Vector2I(to.X, from.Y), width);
                    CarveLine(new Vector2I(to.X, from.Y), to, width);
                }
                else
                {
                    CarveLine(from, new Vector2I(from.X, to.Y), width);
                    CarveLine(new Vector2I(from.X, to.Y), to, width);
                }
                break;
            case CorridorStyle.Z:
                var mid = new Vector2I(((from.X + to.X) / 2) + _rng.Next(-2, 3), ((from.Y + to.Y) / 2) + _rng.Next(-2, 3));
                CarveLine(from, new Vector2I(mid.X, from.Y), width);
                CarveLine(new Vector2I(mid.X, from.Y), mid, width);
                CarveLine(mid, new Vector2I(to.X, mid.Y), width);
                CarveLine(new Vector2I(to.X, mid.Y), to, width);
                break;
            case CorridorStyle.Diag:
                CarveDiagonal(from, to);
                break;
            default:
                CarveDrunk(from, to);
                break;
        }
    }

    private CorridorStyle ChooseCorridorStyle()
    {
        var roll = _rng.NextDouble();
        if (roll < 0.4) return CorridorStyle.L;
        if (roll < 0.7) return CorridorStyle.Z;
        if (roll < 0.9) return CorridorStyle.Drunk;
        return CorridorStyle.Diag;
    }

    private void CarveLine(Vector2I from, Vector2I to, int width)
    {
        if (from.X == to.X)
        {
            var step = to.Y >= from.Y ? 1 : -1;
            for (var y = from.Y; y != to.Y + step; y += step)
            {
                for (var dx = -width + 1; dx <= width - 1; dx++)
                {
                    CarveFloor(new Vector2I(from.X + dx, y));
                }
            }

            return;
        }

        var xStep = to.X >= from.X ? 1 : -1;
        for (var x = from.X; x != to.X + xStep; x += xStep)
        {
            for (var dy = -width + 1; dy <= width - 1; dy++)
            {
                CarveFloor(new Vector2I(x, from.Y + dy));
            }
        }
    }

    private void CarveDiagonal(Vector2I from, Vector2I to)
    {
        var pos = from;
        for (var i = 0; i < 80; i++)
        {
            CarveFloor(pos);
            if (pos == to) break;
            if (pos.X != to.X) pos.X += Math.Sign(to.X - pos.X);
            if (pos.Y != to.Y) pos.Y += Math.Sign(to.Y - pos.Y);
            pos = ClampInside(pos);
        }
    }

    private void CarveDrunk(Vector2I from, Vector2I to)
    {
        var pos = from;
        for (var i = 0; i < 60; i++)
        {
            CarveFloor(pos);
            if (pos == to) break;
            if (_rng.NextDouble() < 0.5)
            {
                pos.X += Math.Sign(to.X - pos.X);
            }
            else
            {
                pos.Y += Math.Sign(to.Y - pos.Y);
            }

            pos = ClampInside(pos);
        }
    }
}
