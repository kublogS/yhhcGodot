using System;
using System.Collections.Generic;
using Godot;

public sealed partial class ProceduralTilemapBuilder
{
    private enum RoomConnectionKind
    {
        NarrowDoor,
        WideDoor,
        ArchOpen,
        WallBreach,
        ShortVestibule,
    }

    private const int MaxCompactConnectionLength = 9;

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
                var kind = ChooseConnectionKind(a, b);
                BuildRoomConnection(a, b, dir, kind);
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

    private RoomConnectionKind ChooseConnectionKind(Vector2I a, Vector2I b)
    {
        var distance = Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        if (distance > MaxCompactConnectionLength)
        {
            return RoomConnectionKind.ShortVestibule;
        }

        var roll = _rng.NextDouble();
        if (roll < 0.34) return RoomConnectionKind.NarrowDoor;
        if (roll < 0.64) return RoomConnectionKind.WideDoor;
        if (roll < 0.83) return RoomConnectionKind.ArchOpen;
        if (roll < 0.95) return RoomConnectionKind.WallBreach;
        return RoomConnectionKind.ShortVestibule;
    }

    private void BuildRoomConnection(Vector2I a, Vector2I b, char dir, RoomConnectionKind kind)
    {
        var outward = DirectionVector(dir);
        var inward = DirectionVector(Opposite(dir));
        OpenAnchor(a, outward, kind);
        OpenAnchor(b, inward, kind);

        var start = a + outward;
        var end = b + inward;
        if (!InBounds(start, 1) || !InBounds(end, 1))
        {
            return;
        }

        var distance = Math.Abs(start.X - end.X) + Math.Abs(start.Y - end.Y);
        if (distance > MaxCompactConnectionLength)
        {
            CarveLegacyFallback(start, end);
            return;
        }

        var width = kind is RoomConnectionKind.WideDoor or RoomConnectionKind.ArchOpen ? 2 : 1;
        CarveConnector(start, end, width);
        if (kind == RoomConnectionKind.WallBreach)
        {
            ScatterBreakableAtOpening(a, outward);
            ScatterBreakableAtOpening(b, inward);
        }
    }

    private void OpenAnchor(Vector2I anchor, Vector2I outward, RoomConnectionKind kind)
    {
        var tile = kind is RoomConnectionKind.NarrowDoor or RoomConnectionKind.ShortVestibule
            ? TileType.Doorway
            : TileType.Threshold;
        SetTile(anchor, tile);
        SetTile(anchor - outward, TileType.Floor);
        SetTile(anchor + outward, TileType.Threshold);

        if (kind is not (RoomConnectionKind.WideDoor or RoomConnectionKind.ArchOpen))
        {
            return;
        }

        var side = new Vector2I(outward.Y, outward.X);
        SetTile(anchor + side, TileType.Threshold);
        SetTile(anchor - side, TileType.Threshold);
    }

    private void CarveConnector(Vector2I from, Vector2I to, int width)
    {
        if (from.X == to.X || from.Y == to.Y)
        {
            CarveLine(from, to, width);
            return;
        }

        var horizontalFirst = _rng.NextDouble() < 0.5;
        var midA = horizontalFirst ? new Vector2I(to.X, from.Y) : new Vector2I(from.X, to.Y);
        var midB = horizontalFirst ? new Vector2I(from.X, to.Y) : new Vector2I(to.X, from.Y);
        var mid = InBounds(midA, 1) ? midA : midB;
        CarveLine(from, mid, width);
        CarveLine(mid, to, width);
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

    private void CarveLegacyFallback(Vector2I from, Vector2I to)
    {
        var pivot = _rng.NextDouble() < 0.5
            ? new Vector2I(to.X, from.Y)
            : new Vector2I(from.X, to.Y);
        CarveLine(from, pivot, 1);
        CarveLine(pivot, to, 1);
    }

    private void ScatterBreakableAtOpening(Vector2I anchor, Vector2I outward)
    {
        var side = new Vector2I(outward.Y, outward.X);
        var a = anchor + side;
        var b = anchor - side;
        TryMarkBreakable(a);
        TryMarkBreakable(b);
    }

    private void TryMarkBreakable(Vector2I pos)
    {
        if (!InBounds(pos, 1) || _grid[pos.Y, pos.X] != (int)TileType.Wall || _rng.NextDouble() > 0.22)
        {
            return;
        }

        _grid[pos.Y, pos.X] = (int)TileType.Breakable;
        _breakableTiles.Add(pos);
    }
}
