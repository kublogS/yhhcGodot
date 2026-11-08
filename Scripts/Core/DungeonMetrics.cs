using System;
using Godot;

public static class DungeonMetrics
{
    public const float TileSize = 2.0f;

    public static Vector2I WorldToGrid(Vector3 worldPos, float tileSize = TileSize)
    {
        var safeTileSize = MathF.Max(0.01f, tileSize);
        return new Vector2I(
            (int)MathF.Round(worldPos.X / safeTileSize),
            (int)MathF.Round(worldPos.Z / safeTileSize));
    }

    public static Vector3 GridToWorld(int x, int y, float tileSize = TileSize)
    {
        var safeTileSize = MathF.Max(0.01f, tileSize);
        return new Vector3(x * safeTileSize, 0.0f, y * safeTileSize);
    }
}
