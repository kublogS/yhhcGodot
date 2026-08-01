using System;
using System.Collections.Generic;
using Godot;

public sealed class ProceduralTilemapBuildContext
{
    public const int NormalRoomWidth = 13;
    public const int NormalRoomHeight = 9;
    public const int BossRoomWidth = 17;
    public const int BossRoomHeight = 11;
    private const int CellStepX = 20;
    private const int CellStepY = 14;

    public ProcRoomGraph Graph { get; }
    public ProcEmbedResult Embed { get; }
    public Random Rng { get; }
    public int Gap { get; }
    public int[,] Grid { get; }
    public int[,] RoomIdGrid { get; }
    public Dictionary<int, Rect2I> RoomBounds { get; } = new();
    public Dictionary<int, int> RoomLevels { get; } = new();
    public Dictionary<int, List<int>> RoomNeighbors { get; } = new();
    public Dictionary<int, RoomBoundaryDescriptor> RoomBoundaryDescriptors { get; } = new();
    public HashSet<Vector2I> CorridorTiles { get; } = new();
    public HashSet<Vector2I> BreakableTiles { get; } = new();
    public HashSet<Vector2I> ExitTiles { get; } = new();
    public HashSet<Vector2I> SaveTiles { get; } = new();
    public HashSet<int> SaveRoomIds { get; } = new();

    public int Width => Grid.GetLength(1);
    public int Height => Grid.GetLength(0);

    public ProceduralTilemapBuildContext(ProcRoomGraph graph, ProcEmbedResult embed, int seed, int gap)
    {
        Graph = graph;
        Embed = embed;
        Rng = new Random(seed);
        Gap = Math.Max(0, gap);
        var width = (embed.Width * CellStepX) + 10 + Gap;
        var height = (embed.Height * CellStepY) + 10 + Gap;
        Grid = new int[height, width];
        RoomIdGrid = new int[height, width];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                RoomIdGrid[y, x] = -1;
            }
        }
    }

    public Vector2I ResolveRoomSize(ProcRoomType type)
    {
        return type == ProcRoomType.Boss
            ? new Vector2I(BossRoomWidth, BossRoomHeight)
            : new Vector2I(NormalRoomWidth, NormalRoomHeight);
    }

    public Vector2I ResolveRoomOrigin(Vector2I embedCell, Vector2I roomSize)
    {
        var rawX = 4 + (embedCell.X * CellStepX);
        var rawY = 4 + (embedCell.Y * CellStepY);
        var maxX = Math.Max(1, Width - roomSize.X - 1);
        var maxY = Math.Max(1, Height - roomSize.Y - 1);
        return new Vector2I(Math.Clamp(rawX, 1, maxX), Math.Clamp(rawY, 1, maxY));
    }

    public bool InBounds(int x, int y, int border = 0)
    {
        return x >= border && y >= border && x < Width - border && y < Height - border;
    }

    public void SetTile(int x, int y, TileType tile)
    {
        if (InBounds(x, y))
        {
            Grid[y, x] = (int)tile;
        }
    }

    public TileType GetTile(int x, int y)
    {
        if (!InBounds(x, y))
        {
            return TileType.Wall;
        }

        return (TileType)Grid[y, x];
    }

    public Vector2I RoomCenter(Rect2I bounds)
    {
        return new Vector2I(bounds.Position.X + (bounds.Size.X / 2), bounds.Position.Y + (bounds.Size.Y / 2));
    }

    public void RegisterNeighbor(int a, int b)
    {
        if (!RoomNeighbors.TryGetValue(a, out var listA))
        {
            listA = new List<int>();
            RoomNeighbors[a] = listA;
        }

        if (!RoomNeighbors.TryGetValue(b, out var listB))
        {
            listB = new List<int>();
            RoomNeighbors[b] = listB;
        }

        if (!listA.Contains(b)) listA.Add(b);
        if (!listB.Contains(a)) listB.Add(a);
    }

    public ProcTilemapResult ToResult()
    {
        return new ProcTilemapResult
        {
            Grid = Grid,
            RoomIdGrid = RoomIdGrid,
            RoomBounds = RoomBounds,
            RoomLevels = RoomLevels,
            RoomNeighbors = RoomNeighbors,
            RoomBoundaryDescriptors = RoomBoundaryDescriptors,
            CorridorTiles = CorridorTiles,
            BreakableTiles = BreakableTiles,
            ExitTiles = ExitTiles,
            SaveTiles = SaveTiles,
            SaveRoomIds = SaveRoomIds,
        };
    }
}
