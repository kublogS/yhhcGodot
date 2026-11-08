using System.Collections.Generic;
using Godot;

public enum TileType
{
    Void = 0,
    Floor = 1,
    Wall = 2,
    Doorway = 3,
    Door = Doorway,
    Breakable = 4,
    Exit = 5,
    Threshold = 6,
    Save = 7,
}

public sealed class DungeonData
{
    public int[,] Grid { get; set; } = new int[1, 1];
    public int[,] RoomIdGrid { get; set; } = new int[1, 1];
    public Dictionary<int, Rect2I> RoomBounds { get; set; } = new();
    public Dictionary<int, List<int>> RoomNeighbors { get; set; } = new();
    public Dictionary<int, RoomBoundaryDescriptor> RoomBoundaryDescriptors { get; set; } = new();
    public Dictionary<int, int> RoomLevels { get; set; } = new();
    public HashSet<Vector2I> CorridorTiles { get; set; } = new();
    public HashSet<Vector2I> BreakableTiles { get; set; } = new();
    public HashSet<Vector2I> BrokenBreakableTiles { get; set; } = new();
    public HashSet<Vector2I> ExitTiles { get; set; } = new();
    public HashSet<Vector2I> SaveTiles { get; set; } = new();
    public HashSet<int> SaveRoomIds { get; set; } = new();
    public Vector3 PlayerSpawn { get; set; } = Vector3.Zero;
    public List<Vector3> EnemySpawns { get; set; } = new();
    public Vector3 FloorBossSpawn { get; set; } = Vector3.Zero;
    public bool HasFloorBossSpawn { get; set; }
    public Vector3 ExitPosition { get; set; } = Vector3.Zero;
    public int Seed { get; set; }
    public int FloorIndex { get; set; }
    public int StartRoomId { get; set; }
    public int BossRoomId { get; set; }
    public bool LayoutTuned { get; set; }

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
        return CanTraverse(x, y);
    }

    public bool CanTraverse(int x, int y)
    {
        var state = GetTraversalState(x, y);
        return state is TileTraversalState.Walkable or TileTraversalState.BreakableBroken;
    }

    public TileTraversalState GetTraversalState(int x, int y)
    {
        if (BrokenBreakableTiles.Contains(new Vector2I(x, y)))
        {
            return TileTraversalState.BreakableBroken;
        }

        var tile = GetTile(x, y);
        return DungeonWalkabilityPolicy.Resolve(tile, isBreakableBroken: false);
    }

    public bool TryBreakBreakable(int x, int y)
    {
        if (GetTile(x, y) != TileType.Breakable)
        {
            return false;
        }

        var tile = new Vector2I(x, y);
        BreakableTiles.Remove(tile);
        BrokenBreakableTiles.Add(tile);
        Grid[y, x] = (int)TileType.Floor;
        return true;
    }
}
