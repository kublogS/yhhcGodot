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
    public int[,] RoomIdGrid { get; set; } = new int[1, 1];
    public Dictionary<int, Rect2I> RoomBounds { get; set; } = new();
    public Dictionary<int, List<int>> RoomNeighbors { get; set; } = new();
    public Dictionary<int, int> RoomLevels { get; set; } = new();
    public HashSet<Vector2I> CorridorTiles { get; set; } = new();
    public HashSet<Vector2I> BreakableTiles { get; set; } = new();
    public HashSet<Vector2I> ExitTiles { get; set; } = new();
    public Vector3 PlayerSpawn { get; set; } = Vector3.Zero;
    public List<Vector3> EnemySpawns { get; set; } = new();
    public Vector3 FloorBossSpawn { get; set; } = Vector3.Zero;
    public bool HasFloorBossSpawn { get; set; }
    public Vector3 ExitPosition { get; set; } = Vector3.Zero;
    public int Seed { get; set; }
    public int FloorIndex { get; set; }
    public int StartRoomId { get; set; }
    public int BossRoomId { get; set; }

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

    public int RoomIdAt(int x, int y)
    {
        if (RoomIdGrid.GetLength(0) != Height || RoomIdGrid.GetLength(1) != Width)
        {
            return -1;
        }

        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return -1;
        }

        return RoomIdGrid[y, x];
    }

    public bool IsWalkable(int x, int y)
    {
        var tile = GetTile(x, y);
        return tile is TileType.Floor or TileType.Door or TileType.Exit or TileType.Breakable;
    }
}
