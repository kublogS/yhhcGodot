using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class ProceduralGraphEmbedder
{
    private static readonly (char Dir, Vector2I Delta)[] Directions =
    {
        ('N', new Vector2I(0, -1)),
        ('S', new Vector2I(0, 1)),
        ('W', new Vector2I(-1, 0)),
        ('E', new Vector2I(1, 0)),
    };

    public static ProcEmbedResult Place(ProcRoomGraph graph, int size, int seed)
    {
        var rng = new Random(seed);
        var result = new ProcEmbedResult { Width = size, Height = size };
        var center = new Vector2I(size / 2, size / 2);
        result.Positions[graph.StartId] = center;
        result.Doors[graph.StartId] = new Dictionary<char, int>();

        bool TryPlace(int nodeId)
        {
            var neighbors = graph.Nodes[nodeId].Neighbors.ToList();
            Shuffle(rng, neighbors);
            foreach (var neighborId in neighbors)
            {
                if (result.Positions.ContainsKey(neighborId))
                {
                    continue;
                }

                var dirs = Directions.ToList();
                Shuffle(rng, dirs);
                var placed = false;
                foreach (var (dir, delta) in dirs)
                {
                    var target = result.Positions[nodeId] + delta;
                    if (target.X < 0 || target.Y < 0 || target.X >= size || target.Y >= size)
                    {
                        continue;
                    }

                    if (result.Positions.Values.Contains(target))
                    {
                        continue;
                    }

                    result.Positions[neighborId] = target;
                    AddDoor(result, nodeId, dir, neighborId);
                    AddDoor(result, neighborId, Opposite(dir), nodeId);
                    if (TryPlace(neighborId))
                    {
                        placed = true;
                        break;
                    }

                    result.Positions.Remove(neighborId);
                    RemoveDoor(result, nodeId, dir);
                    RemoveDoor(result, neighborId, Opposite(dir));
                }

                if (!placed)
                {
                    return false;
                }
            }

            return true;
        }

        if (!TryPlace(graph.StartId))
        {
            FallbackLinear(graph, result, center, size);
        }

        return result;
    }

    private static void FallbackLinear(ProcRoomGraph graph, ProcEmbedResult result, Vector2I center, int size)
    {
        result.Positions.Clear();
        result.Doors.Clear();
        var ids = graph.Nodes.Keys.OrderBy(id => id).ToList();
        var x = center.X;
        var y = center.Y;
        for (var i = 0; i < ids.Count; i++)
        {
            var id = ids[i];
            result.Positions[id] = new Vector2I(Math.Clamp(x, 0, size - 1), Math.Clamp(y, 0, size - 1));
            if (i > 0)
            {
                var prev = ids[i - 1];
                AddDoor(result, prev, 'E', id);
                AddDoor(result, id, 'W', prev);
            }

            x++;
            if (x >= size)
            {
                x = 0;
                y = (y + 1) % size;
            }
        }
    }

    private static void AddDoor(ProcEmbedResult result, int roomId, char dir, int targetId)
    {
        if (!result.Doors.TryGetValue(roomId, out var map))
        {
            map = new Dictionary<char, int>();
            result.Doors[roomId] = map;
        }

        map[dir] = targetId;
    }

    private static void RemoveDoor(ProcEmbedResult result, int roomId, char dir)
    {
        if (result.Doors.TryGetValue(roomId, out var map))
        {
            map.Remove(dir);
        }
    }

    private static char Opposite(char dir)
    {
        return dir switch
        {
            'N' => 'S',
            'S' => 'N',
            'W' => 'E',
            _ => 'W',
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
