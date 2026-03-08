using System;
using System.Collections.Generic;
using Godot;

public sealed partial class ProceduralTilemapBuilder
{
    private readonly ProcRoomGraph _graph;
    private readonly ProcEmbedResult _embed;
    private readonly Random _rng;
    private readonly int _gap;
    private readonly Dictionary<int, Vector2I> _roomSizes = new();
    private readonly Dictionary<int, Rect2I> _roomBounds = new();
    private readonly Dictionary<int, Dictionary<char, Vector2I>> _doorSockets = new();
    private readonly Dictionary<int, int> _roomLevels = new();
    private readonly Dictionary<int, List<int>> _roomNeighbors = new();
    private readonly HashSet<Vector2I> _corridorTiles = new();
    private readonly HashSet<Vector2I> _breakableTiles = new();
    private readonly HashSet<Vector2I> _exitTiles = new();

    private int[,] _grid = new int[1, 1];
    private int[,] _roomIdGrid = new int[1, 1];
    private int _gridWidth;
    private int _gridHeight;
    private int _maxRoomWidth;
    private int _maxRoomHeight;

    public ProceduralTilemapBuilder(ProcRoomGraph graph, ProcEmbedResult embed, int seed, int gap = 1)
    {
        _graph = graph;
        _embed = embed;
        _gap = Math.Max(1, gap);
        _rng = new Random(seed);
    }

    public ProcTilemapResult Build()
    {
        AllocateGrids();
        PaintRooms();
        ConnectRooms();
        AddCorridorBranches();
        AddSecretRooms();
        AddExitTile();
        return new ProcTilemapResult
        {
            Grid = _grid,
            RoomIdGrid = _roomIdGrid,
            RoomBounds = _roomBounds,
            RoomLevels = _roomLevels,
            RoomNeighbors = _roomNeighbors,
            CorridorTiles = _corridorTiles,
            BreakableTiles = _breakableTiles,
            ExitTiles = _exitTiles,
        };
    }

    private void AllocateGrids()
    {
        foreach (var node in _graph.Nodes.Values)
        {
            var size = node.Type == ProcRoomType.Boss ? new Vector2I(19, 13) : new Vector2I(15, 9);
            _roomSizes[node.Id] = size;
            _maxRoomWidth = Math.Max(_maxRoomWidth, size.X);
            _maxRoomHeight = Math.Max(_maxRoomHeight, size.Y);
        }

        _gridWidth = _embed.Width * _maxRoomWidth + ((_embed.Width - 1) * _gap);
        _gridHeight = _embed.Height * _maxRoomHeight + ((_embed.Height - 1) * _gap);
        _grid = new int[_gridHeight, _gridWidth];
        _roomIdGrid = new int[_gridHeight, _gridWidth];
        for (var y = 0; y < _gridHeight; y++)
        {
            for (var x = 0; x < _gridWidth; x++)
            {
                _roomIdGrid[y, x] = -1;
            }
        }
    }

    private void PaintRooms()
    {
        foreach (var (roomId, gridPos) in _embed.Positions)
        {
            var roomSize = _roomSizes[roomId];
            var ox = gridPos.X * (_maxRoomWidth + _gap);
            var oy = gridPos.Y * (_maxRoomHeight + _gap);
            _roomBounds[roomId] = new Rect2I(ox, oy, roomSize.X, roomSize.Y);
            _roomLevels[roomId] = (_graph.Nodes[roomId].Type == ProcRoomType.Boss || roomId % 7 == 0) ? 1 : 0;
            var roomTiles = BuildRoomTiles(roomSize.X, roomSize.Y);
            for (var y = 0; y < roomSize.Y; y++)
            {
                for (var x = 0; x < roomSize.X; x++)
                {
                    _grid[oy + y, ox + x] = roomTiles[y, x];
                    _roomIdGrid[oy + y, ox + x] = roomId;
                }
            }

            _doorSockets[roomId] = new Dictionary<char, Vector2I>
            {
                ['N'] = new Vector2I(DoorOffset(roomSize.X), 0),
                ['S'] = new Vector2I(DoorOffset(roomSize.X), roomSize.Y - 1),
                ['W'] = new Vector2I(0, DoorOffset(roomSize.Y)),
                ['E'] = new Vector2I(roomSize.X - 1, DoorOffset(roomSize.Y)),
            };
        }
    }

    private int[,] BuildRoomTiles(int width, int height)
    {
        var tiles = new int[height, width];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                tiles[y, x] = (x == 0 || y == 0 || x == width - 1 || y == height - 1) ? (int)TileType.Wall : (int)TileType.Floor;
            }
        }

        for (var i = 0; i < _rng.Next(1, 4); i++)
        {
            ApplyCutout(tiles, width, height);
        }

        for (var i = 0; i < _rng.Next(0, 4); i++)
        {
            tiles[_rng.Next(2, height - 2), _rng.Next(2, width - 2)] = (int)TileType.Wall;
        }

        return tiles;
    }

    private void ApplyCutout(int[,] tiles, int width, int height)
    {
        var side = _rng.Next(4);
        var cutWidth = _rng.Next(2, 5);
        var cutHeight = _rng.Next(2, 4);
        if (side == 0 || side == 1)
        {
            var sx = _rng.Next(1, width - cutWidth - 1);
            var startY = side == 0 ? 1 : height - cutHeight - 1;
            for (var y = startY; y < startY + cutHeight; y++)
            {
                for (var x = sx; x < sx + cutWidth; x++)
                {
                    tiles[y, x] = (int)TileType.Wall;
                }
            }

            return;
        }

        var sy = _rng.Next(1, height - cutHeight - 1);
        var startX = side == 2 ? 1 : width - cutWidth - 1;
        for (var y = sy; y < sy + cutHeight; y++)
        {
            for (var x = startX; x < startX + cutWidth; x++)
            {
                tiles[y, x] = (int)TileType.Wall;
            }
        }
    }

    private int DoorOffset(int length)
    {
        var roll = _rng.NextDouble();
        if (roll < 0.4) return 2 + _rng.Next(0, 2);
        if (roll < 0.8) return length - 3 - _rng.Next(0, 2);
        return (length / 2) + _rng.Next(-1, 2);
    }
}
