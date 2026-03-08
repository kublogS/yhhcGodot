using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static partial class DungeonGenerator
{
    private sealed class Room
    {
        public int X;
        public int Y;
        public int W;
        public int H;

        public int CenterX => X + (W / 2);
        public int CenterY => Y + (H / 2);
    }

    public static DungeonData Generate(GameRng rng, int width = 45, int height = 45, int roomCount = 11)
    {
        width = Math.Max(21, width | 1);
        height = Math.Max(21, height | 1);

        var grid = new int[height, width];
        Fill(grid, (int)TileType.Wall);

        var rooms = new List<Room>();
        for (var i = 0; i < roomCount; i++)
        {
            var room = new Room
            {
                W = rng.NextInt(7, 11),
                H = rng.NextInt(7, 11),
            };
            room.X = rng.NextInt(1, width - room.W - 2);
            room.Y = rng.NextInt(1, height - room.H - 2);
            if (Overlaps(rooms, room))
            {
                continue;
            }

            CarveRoom(grid, room);
            if (rooms.Count > 0)
            {
                CarveCorridor(grid, rooms[^1].CenterX, rooms[^1].CenterY, room.CenterX, room.CenterY);
            }

            rooms.Add(room);
        }

        if (rooms.Count == 0)
        {
            var fallback = new Room { X = width / 2 - 3, Y = height / 2 - 3, W = 7, H = 7 };
            CarveRoom(grid, fallback);
            rooms.Add(fallback);
        }

        var spawn = rooms[0];
        var farRoom = rooms.OrderByDescending(r => DistanceSquared(r.CenterX, r.CenterY, spawn.CenterX, spawn.CenterY)).First();
        grid[farRoom.CenterY, farRoom.CenterX] = (int)TileType.Exit;

        var enemySpawns = new List<Vector3>();
        foreach (var room in rooms.Skip(1))
        {
            var enemies = rng.NextInt(0, 2);
            for (var i = 0; i < enemies; i++)
            {
                var x = rng.NextInt(room.X + 1, room.X + room.W - 2);
                var y = rng.NextInt(room.Y + 1, room.Y + room.H - 2);
                if (grid[y, x] == (int)TileType.Floor)
                {
                    enemySpawns.Add(GridToWorld(x, y, DungeonBuilder.TileSize));
                }
            }
        }

        if (enemySpawns.Count == 0)
        {
            enemySpawns.Add(GridToWorld(spawn.CenterX + 2, spawn.CenterY + 2, DungeonBuilder.TileSize));
        }

        var dungeon = new DungeonData
        {
            Grid = grid,
            PlayerSpawn = GridToWorld(spawn.CenterX, spawn.CenterY, DungeonBuilder.TileSize),
            ExitPosition = GridToWorld(farRoom.CenterX, farRoom.CenterY, DungeonBuilder.TileSize),
            EnemySpawns = enemySpawns,
        };
        DungeonLayoutTuner.EnsureComfortablePassages(dungeon);
        return dungeon;
    }

    public static Vector2I WorldToGrid(Vector3 worldPos, float tileSize = 2f)
    {
        return new Vector2I((int)MathF.Round(worldPos.X / tileSize), (int)MathF.Round(worldPos.Z / tileSize));
    }

    public static Vector3 GridToWorld(int x, int y, float tileSize = 2f)
    {
        return new Vector3(x * tileSize, 0.0f, y * tileSize);
    }

    private static void Fill(int[,] grid, int value)
    {
        for (var y = 0; y < grid.GetLength(0); y++)
        {
            for (var x = 0; x < grid.GetLength(1); x++)
            {
                grid[y, x] = value;
            }
        }
    }

    private static bool Overlaps(List<Room> rooms, Room candidate)
    {
        foreach (var room in rooms)
        {
            if (candidate.X < room.X + room.W + 1
                && candidate.X + candidate.W + 1 > room.X
                && candidate.Y < room.Y + room.H + 1
                && candidate.Y + candidate.H + 1 > room.Y)
            {
                return true;
            }
        }

        return false;
    }

    private static void CarveRoom(int[,] grid, Room room)
    {
        for (var y = room.Y; y < room.Y + room.H; y++)
        {
            for (var x = room.X; x < room.X + room.W; x++)
            {
                grid[y, x] = (x == room.X || y == room.Y || x == room.X + room.W - 1 || y == room.Y + room.H - 1)
                    ? (int)TileType.Wall
                    : (int)TileType.Floor;
            }
        }

        CarveDoorway(grid, room.X, room.CenterY);
        CarveDoorway(grid, room.X + room.W - 1, room.CenterY);
    }

    private static int DistanceSquared(int x1, int y1, int x2, int y2)
    {
        var dx = x1 - x2;
        var dy = y1 - y2;
        return dx * dx + dy * dy;
    }
}
