using System.Collections.Generic;
using Godot;

public static partial class ProceduralGenerationValidator
{
    public static List<string> ValidateDungeon(ProcRoomGraph graph, DungeonData dungeon)
    {
        var errors = new List<string>();
        if (dungeon.Width < 3 || dungeon.Height < 3)
        {
            errors.Add("grid too small");
            return errors;
        }

        var startTile = DungeonGenerator.WorldToGrid(dungeon.PlayerSpawn, DungeonBuilder.TileSize);
        if (!dungeon.IsWalkable(startTile.X, startTile.Y))
        {
            errors.Add("spawn not walkable");
        }

        if (!dungeon.RoomBounds.TryGetValue(graph.BossId, out var bossRoom))
        {
            errors.Add("boss room bounds missing");
            return errors;
        }

        var bossTile = new Vector2I(bossRoom.Position.X + (bossRoom.Size.X / 2), bossRoom.Position.Y + (bossRoom.Size.Y / 2));
        if (!dungeon.IsWalkable(bossTile.X, bossTile.Y))
        {
            errors.Add("boss center not walkable");
            return errors;
        }

        var reachable = FloodWalkable(dungeon, startTile);
        if (!reachable.Contains(bossTile))
        {
            errors.Add("boss unreachable from start");
        }

        foreach (var node in graph.Nodes.Values)
        {
            if (!node.MainPath)
            {
                continue;
            }

            if (!dungeon.RoomBounds.TryGetValue(node.Id, out var room))
            {
                errors.Add($"missing room bounds {node.Id}");
                continue;
            }

            var center = new Vector2I(room.Position.X + (room.Size.X / 2), room.Position.Y + (room.Size.Y / 2));
            if (!dungeon.IsWalkable(center.X, center.Y))
            {
                center = FindNearestWalkableInRoom(dungeon, room);
            }

            if (center.X < 0 || !reachable.Contains(center))
            {
                errors.Add($"room {node.Id} unreachable");
            }
        }

        return errors;
    }

    private static HashSet<Vector2I> FloodWalkable(DungeonData dungeon, Vector2I start)
    {
        var visited = new HashSet<Vector2I>();
        if (!dungeon.IsWalkable(start.X, start.Y))
        {
            return visited;
        }

        var queue = new Queue<Vector2I>();
        queue.Enqueue(start);
        visited.Add(start);
        var dirs = new[] { new Vector2I(1, 0), new Vector2I(-1, 0), new Vector2I(0, 1), new Vector2I(0, -1) };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var dir in dirs)
            {
                var next = current + dir;
                if (next.X < 0 || next.Y < 0 || next.X >= dungeon.Width || next.Y >= dungeon.Height)
                {
                    continue;
                }

                if (visited.Contains(next) || !dungeon.IsWalkable(next.X, next.Y))
                {
                    continue;
                }

                visited.Add(next);
                queue.Enqueue(next);
            }
        }

        return visited;
    }

    private static Vector2I FindNearestWalkableInRoom(DungeonData dungeon, Rect2I room)
    {
        for (var y = room.Position.Y + 1; y < room.End.Y - 1; y++)
        {
            for (var x = room.Position.X + 1; x < room.End.X - 1; x++)
            {
                if (dungeon.IsWalkable(x, y))
                {
                    return new Vector2I(x, y);
                }
            }
        }

        return new Vector2I(-1, -1);
    }
}
