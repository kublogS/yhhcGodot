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
    private readonly Dictionary<int, RoomBoundaryDescriptor> _roomBoundaryDescriptors = new();
    private readonly HashSet<Vector2I> _corridorTiles = new();
    private readonly HashSet<Vector2I> _breakableTiles = new();
    private readonly HashSet<Vector2I> _exitTiles = new();
    private readonly HashSet<Vector2I> _saveTiles = new();
    private readonly HashSet<int> _saveRoomIds = new();

    private int[,] _grid = new int[1, 1];
    private int[,] _roomIdGrid = new int[1, 1];
    private int _gridWidth;
    private int _gridHeight;
    private int _maxRoomWidth;
    private int _maxRoomHeight;
    private int _cellStepX;
    private int _cellStepY;

    public ProceduralTilemapBuilder(ProcRoomGraph graph, ProcEmbedResult embed, int seed, int gap = 1)
    {
        _graph = graph;
        _embed = embed;
        _gap = Math.Max(0, gap);
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
        AddSaveSanctuaries();
        FinalizeTopology();
        return new ProcTilemapResult
        {
            Grid = _grid,
            RoomIdGrid = _roomIdGrid,
            RoomBounds = _roomBounds,
            RoomLevels = _roomLevels,
            RoomNeighbors = _roomNeighbors,
            RoomBoundaryDescriptors = _roomBoundaryDescriptors,
            CorridorTiles = _corridorTiles,
            BreakableTiles = _breakableTiles,
            ExitTiles = _exitTiles,
            SaveTiles = _saveTiles,
            SaveRoomIds = _saveRoomIds,
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

        _cellStepX = Math.Max(8, _maxRoomWidth - 3);
        _cellStepY = Math.Max(6, _maxRoomHeight - 2);
        _gridWidth = ((_embed.Width - 1) * _cellStepX) + _maxRoomWidth + 4 + _gap;
        _gridHeight = ((_embed.Height - 1) * _cellStepY) + _maxRoomHeight + 4 + _gap;
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
            var roomType = _graph.Nodes[roomId].Type;
            var profile = RoomShapeProfile.Create(_rng, roomType == ProcRoomType.Boss);
            _roomBoundaryDescriptors[roomId] = profile.ToDescriptor(roomId);

            var ox = 2 + (gridPos.X * _cellStepX) + _rng.Next(-1, 2);
            var oy = 2 + (gridPos.Y * _cellStepY) + _rng.Next(-1, 2);
            ox = Math.Clamp(ox, 1, _gridWidth - roomSize.X - 1);
            oy = Math.Clamp(oy, 1, _gridHeight - roomSize.Y - 1);
            _roomBounds[roomId] = new Rect2I(ox, oy, roomSize.X, roomSize.Y);
            _roomLevels[roomId] = (roomType == ProcRoomType.Boss || roomId % 7 == 0) ? 1 : 0;
            var roomTiles = RoomShapeMutator.BuildTiles(roomSize.X, roomSize.Y, _rng, profile);
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
            EnsureSocketInterior(roomId, ox, oy);
        }
    }

    private void EnsureSocketInterior(int roomId, int ox, int oy)
    {
        foreach (var (dir, local) in _doorSockets[roomId])
        {
            var socket = new Vector2I(ox + local.X, oy + local.Y);
            var inward = DirectionVector(Opposite(dir));
            var insideA = socket + inward;
            var insideB = insideA + inward;
            if (InBounds(socket, 0)) _grid[socket.Y, socket.X] = (int)TileType.Wall;
            if (InBounds(insideA, 0)) _grid[insideA.Y, insideA.X] = (int)TileType.Floor;
            if (InBounds(insideB, 0) && _rng.NextDouble() < 0.55) _grid[insideB.Y, insideB.X] = (int)TileType.Floor;
        }
    }

    private int DoorOffset(int length)
    {
        var roll = _rng.NextDouble();
        if (roll < 0.4) return 2 + _rng.Next(0, 2);
        if (roll < 0.8) return length - 3 - _rng.Next(0, 2);
        return (length / 2) + _rng.Next(-1, 2);
    }

    private static Vector2I DirectionVector(char dir)
    {
        return dir switch
        {
            'N' => new Vector2I(0, -1),
            'S' => new Vector2I(0, 1),
            'W' => new Vector2I(-1, 0),
            _ => new Vector2I(1, 0),
        };
    }
}
