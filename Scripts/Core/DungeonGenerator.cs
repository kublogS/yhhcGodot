using System;
using System.Collections.Generic;
using Godot;

public static partial class DungeonGenerator
{
    private const int EmbedSize = 11;

    public static DungeonData Generate(GameRng rng, int width = 45, int height = 45, int roomCount = 11)
    {
        var seed = rng.NextInt(1, 9_999_999);
        return Generate(seed, 0);
    }

    public static DungeonData Generate(int seed, int floorIndex, ProcGraphParams? graphParams = null)
    {
        var resolvedParams = graphParams ?? ProceduralGraphSettingsFactory.CreateForFloor(seed, floorIndex);
        var graph = ProceduralGraphBuilder.Build(seed, floorIndex, resolvedParams);
        LogValidationErrors("graph", ProceduralGenerationValidator.ValidateGraph(graph));

        var embed = ProceduralGraphEmbedder.Place(graph, EmbedSize, seed);
        LogValidationErrors("embed", ProceduralGenerationValidator.ValidateEmbedding(graph, embed));

        var tilemap = new ProceduralTilemapBuilder(graph, embed, seed, gap: 0).Build();
        var startTile = RoomCenterTile(tilemap.RoomBounds[graph.StartId]);
        var bossTile = RoomCenterTile(tilemap.RoomBounds[graph.BossId]);

        var localRng = new Random(seed + (floorIndex * 31337));
        var enemySpawns = BuildEnemySpawns(graph, tilemap, startTile, localRng);

        var dungeon = new DungeonData
        {
            Grid = tilemap.Grid,
            RoomIdGrid = tilemap.RoomIdGrid,
            RoomBounds = tilemap.RoomBounds,
            RoomNeighbors = tilemap.RoomNeighbors,
            RoomBoundaryDescriptors = tilemap.RoomBoundaryDescriptors,
            RoomLevels = tilemap.RoomLevels,
            CorridorTiles = tilemap.CorridorTiles,
            BreakableTiles = tilemap.BreakableTiles,
            ExitTiles = tilemap.ExitTiles,
            SaveTiles = tilemap.SaveTiles,
            SaveRoomIds = tilemap.SaveRoomIds,
            PlayerSpawn = GridToWorld(startTile.X, startTile.Y, DungeonBuilder.TileSize),
            EnemySpawns = enemySpawns,
            FloorBossSpawn = GridToWorld(bossTile.X, bossTile.Y, DungeonBuilder.TileSize),
            HasFloorBossSpawn = true,
            ExitPosition = GridToWorld(bossTile.X, bossTile.Y, DungeonBuilder.TileSize),
            Seed = seed,
            FloorIndex = floorIndex,
            StartRoomId = graph.StartId,
            BossRoomId = graph.BossId,
        };

        DungeonLayoutTuner.EnsureComfortablePassages(dungeon);
        DungeonLayoutTuner.EnsureWallEnvelope(dungeon);
        dungeon.LayoutTuned = true;
        LogValidationErrors("dungeon", ProceduralGenerationValidator.ValidateDungeon(graph, dungeon));
        dungeon.PlayerSpawn = SnapToWalkable(dungeon, dungeon.PlayerSpawn);
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

    private static Vector2I RoomCenterTile(Rect2I room)
    {
        return new Vector2I(room.Position.X + (room.Size.X / 2), room.Position.Y + (room.Size.Y / 2));
    }

    private static void LogValidationErrors(string stage, List<string> errors)
    {
        if (errors.Count > 0)
        {
            GD.PrintErr($"[Dungeon] {stage} invalid: {string.Join(", ", errors)}");
        }
    }

    private static List<Vector3> BuildEnemySpawns(ProcRoomGraph graph, ProcTilemapResult tilemap, Vector2I startTile, Random rng)
    {
        var spawns = new List<Vector3>();
        foreach (var node in graph.Nodes.Values)
        {
            if (node.Id == graph.StartId || node.Type == ProcRoomType.Boss || !tilemap.RoomBounds.TryGetValue(node.Id, out var bounds))
            {
                continue;
            }

            var enemies = rng.Next(0, 3);
            for (var i = 0; i < enemies; i++)
            {
                var x = rng.Next(bounds.Position.X + 1, bounds.End.X - 1);
                var y = rng.Next(bounds.Position.Y + 1, bounds.End.Y - 1);
                var tile = (TileType)tilemap.Grid[y, x];
                if (tile is not (TileType.Floor or TileType.Doorway or TileType.Threshold) || Math.Abs(x - startTile.X) + Math.Abs(y - startTile.Y) <= 3)
                {
                    continue;
                }

                spawns.Add(GridToWorld(x, y, DungeonBuilder.TileSize));
            }
        }

        return spawns;
    }

    private static Vector3 SnapToWalkable(DungeonData dungeon, Vector3 world)
    {
        var grid = WorldToGrid(world, DungeonBuilder.TileSize);
        if (dungeon.IsWalkable(grid.X, grid.Y))
        {
            return world;
        }

        for (var radius = 1; radius <= 5; radius++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    if (dungeon.IsWalkable(grid.X + x, grid.Y + y))
                    {
                        return GridToWorld(grid.X + x, grid.Y + y, DungeonBuilder.TileSize);
                    }
                }
            }
        }

        return world;
    }
}
