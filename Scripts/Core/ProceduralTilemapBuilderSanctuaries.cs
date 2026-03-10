using System.Collections.Generic;
using System.Linq;
using Godot;

public sealed partial class ProceduralTilemapBuilder
{
    private void AddSaveSanctuaries()
    {
        foreach (var roomId in SelectSanctuaryRoomIds())
        {
            if (!_roomBounds.TryGetValue(roomId, out var room))
            {
                continue;
            }

            var tile = FindSaveTile(room, preferOffset: roomId == _graph.BossId);
            if (!InBounds(tile, 1))
            {
                continue;
            }

            SetTile(tile, TileType.Save);
            _saveTiles.Add(tile);
            _saveRoomIds.Add(roomId);
        }
    }

    private List<int> SelectSanctuaryRoomIds()
    {
        var ordered = _graph.Nodes.Keys
            .Where(id => id != _graph.StartId && id != _graph.BossId)
            .OrderBy(id => id)
            .ToList();
        ordered.Add(_graph.BossId);
        var result = new List<int>();
        if (ordered.Count == 0)
        {
            return result;
        }

        var bossOrder = ordered.Count;
        for (var i = 1; i <= ordered.Count; i++)
        {
            if (i % 6 != 0)
            {
                continue;
            }

            if (bossOrder >= i && bossOrder - i <= 1)
            {
                continue;
            }

            result.Add(ordered[i - 1]);
        }

        if (!result.Contains(_graph.BossId))
        {
            result.Add(_graph.BossId);
        }

        return result;
    }

    private Vector2I FindSaveTile(Rect2I room, bool preferOffset)
    {
        var center = new Vector2I(room.Position.X + (room.Size.X / 2), room.Position.Y + (room.Size.Y / 2));
        var offsets = preferOffset
            ? new[] { new Vector2I(0, 2), new Vector2I(2, 0), new Vector2I(-2, 0), new Vector2I(0, -2), Vector2I.Zero }
            : new[] { Vector2I.Zero, new Vector2I(1, 0), new Vector2I(-1, 0), new Vector2I(0, 1), new Vector2I(0, -1) };
        foreach (var offset in offsets)
        {
            var tile = center + offset;
            if (IsValidSaveTile(tile, room))
            {
                return tile;
            }
        }

        for (var y = room.Position.Y + 1; y < room.End.Y - 1; y++)
        {
            for (var x = room.Position.X + 1; x < room.End.X - 1; x++)
            {
                var tile = new Vector2I(x, y);
                if (IsValidSaveTile(tile, room))
                {
                    return tile;
                }
            }
        }

        return center;
    }

    private bool IsValidSaveTile(Vector2I tile, Rect2I room)
    {
        if (!InBounds(tile, 1) || !room.HasPoint(tile))
        {
            return false;
        }

        var type = (TileType)_grid[tile.Y, tile.X];
        return type is TileType.Floor or TileType.Threshold or TileType.Doorway;
    }
}
