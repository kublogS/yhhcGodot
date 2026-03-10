using System.Collections.Generic;
using Godot;

public enum ProcRoomType
{
    Start,
    Normal,
    Reward,
    Shop,
    Elite,
    Boss,
}

public sealed class ProcRoomNode
{
    public int Id { get; init; }
    public ProcRoomType Type { get; set; } = ProcRoomType.Normal;
    public int Depth { get; init; }
    public bool MainPath { get; init; }
    public int Difficulty { get; set; }
    public int RewardTier { get; set; }
    public HashSet<int> Neighbors { get; } = new();
}

public sealed class ProcRoomGraph
{
    public Dictionary<int, ProcRoomNode> Nodes { get; } = new();
    public int StartId { get; init; }
    public int BossId { get; init; }

    public void AddEdge(int a, int b)
    {
        Nodes[a].Neighbors.Add(b);
        Nodes[b].Neighbors.Add(a);
    }
}

public sealed class ProcGraphParams
{
    public int Depth { get; init; } = 8;
    public float BranchChance { get; init; } = 0.45f;
    public int BranchMin { get; init; } = 1;
    public int BranchMax { get; init; } = 3;
    public int MaxDegree { get; init; } = 3;
    public int ShopDepthMin { get; init; } = 3;
    public int EliteDepthMin { get; init; } = 4;
    public int RewardCooldown { get; init; } = 2;
    public int MinRooms { get; init; } = 10;
    public int MaxRooms { get; init; } = 15;
}

public sealed class ProcEmbedResult
{
    public Dictionary<int, Vector2I> Positions { get; } = new();
    public Dictionary<int, Dictionary<char, int>> Doors { get; } = new();
    public int Width { get; init; }
    public int Height { get; init; }
}

public sealed class ProcTilemapResult
{
    public int[,] Grid { get; init; } = new int[1, 1];
    public int[,] RoomIdGrid { get; init; } = new int[1, 1];
    public Dictionary<int, Rect2I> RoomBounds { get; init; } = new();
    public Dictionary<int, int> RoomLevels { get; init; } = new();
    public Dictionary<int, List<int>> RoomNeighbors { get; init; } = new();
    public Dictionary<int, RoomBoundaryDescriptor> RoomBoundaryDescriptors { get; init; } = new();
    public HashSet<Vector2I> CorridorTiles { get; init; } = new();
    public HashSet<Vector2I> BreakableTiles { get; init; } = new();
    public HashSet<Vector2I> ExitTiles { get; init; } = new();
    public HashSet<Vector2I> SaveTiles { get; init; } = new();
    public HashSet<int> SaveRoomIds { get; init; } = new();
}
