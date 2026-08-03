using System.Collections.Generic;
using Godot;

public static class SavedWorldBreakableCodec
{
    public static List<int> Flatten(HashSet<Vector2I> brokenTiles)
    {
        var data = new List<int>(brokenTiles.Count * 2);
        foreach (var tile in brokenTiles)
        {
            data.Add(tile.X);
            data.Add(tile.Y);
        }

        return data;
    }
}
