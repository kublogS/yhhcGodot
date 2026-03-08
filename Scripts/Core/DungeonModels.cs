using System.Collections.Generic;
using Godot;

public enum TileType
{
    Void = 0,
    Floor = 1,
    Wall = 2,
    Door = 3,
    Breakable = 4,
    Exit = 5,
}

public sealed class DungeonData
{
    public int[,] Grid { get; set; } = new int[1, 1];
    public Vector3 PlayerSpawn { get; set; } = Vector3.Zero;
    public List<Vector3> EnemySpawns { get; set; } = new();
    public Vector3 ExitPosition { get; set; } = Vector3.Zero;
    public int Width => Grid.GetLength(1);
    public int Height => Grid.GetLength(0);

    public TileType GetTile(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return TileType.Wall;
        }

        return (TileType)Grid[y, x];
    }

    public bool IsWalkable(int x, int y)
    {
        var tile = GetTile(x, y);
        return tile == TileType.Floor || tile == TileType.Door || tile == TileType.Exit || tile == TileType.Breakable;
    }
}
