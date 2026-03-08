using Godot;

public partial class ExploreController
{
    private void BuildSpawnHub()
    {
        _inSpawnHub = true;
        _exitTransitionLock = false;
        ClearNodeChildren(_enemiesRoot);
        _manualPanel?.QueueFree();

        var layout = SpawnHubBuilder.Build(_dungeonRoot);
        _spawnDoorInteractPoint = layout.NorthDoorInteractPoint;
        _spawnDoorInteractRadius = layout.NorthDoorInteractRadius;

        _player.GlobalPosition = layout.PlayerSpawn;
        _player.Rotation = new Vector3(0f, 0f, 0f);
        _manual.LoadZone("INDICE");
        _manual.Visible = false;
        _map.Visible = false;
        _pause.Visible = false;
        _manualPanel = ManualPosterBuilder.Build(_dungeonRoot, layout.PosterAnchor, layout.PosterNormal);
    }

    private void CreateManualPanelFromDungeon(DungeonData dungeon)
    {
        _manualPanel?.QueueFree();
        var (anchor, normal) = FindDungeonManualAnchor(dungeon);
        _manualPanel = ManualPosterBuilder.Build(_dungeonRoot, anchor, normal);
    }

    private (Vector3 Position, Vector3 Normal) FindDungeonManualAnchor(DungeonData dungeon)
    {
        var tileSize = DungeonBuilder.TileSize;
        var spawnTile = DungeonGenerator.WorldToGrid(dungeon.PlayerSpawn, tileSize);
        var spawnWorld = DungeonGenerator.GridToWorld(spawnTile.X, spawnTile.Y, tileSize);
        var bestDistance = float.MaxValue;
        var bestPos = spawnWorld + new Vector3(0f, 2.0f, -2.0f);
        var bestNormal = Vector3.Forward;

        for (var y = 0; y < dungeon.Height; y++)
        {
            for (var x = 0; x < dungeon.Width; x++)
            {
                if (dungeon.GetTile(x, y) != TileType.Wall)
                {
                    continue;
                }

                var wallWorld = DungeonGenerator.GridToWorld(x, y, tileSize);
                var normal = (spawnWorld - wallWorld).Slide(Vector3.Up).Normalized();
                if (normal.LengthSquared() < 0.001f)
                {
                    continue;
                }

                var probe = DungeonGenerator.WorldToGrid(wallWorld + normal * tileSize, tileSize);
                if (!dungeon.IsWalkable(probe.X, probe.Y))
                {
                    continue;
                }

                var distance = wallWorld.DistanceSquaredTo(spawnWorld);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestNormal = normal;
                bestPos = wallWorld + (normal * (tileSize * 0.52f)) + new Vector3(0f, 2.0f, 0f);
            }
        }

        return (bestPos, bestNormal);
    }

    private void EnterGeneratedOverworld()
    {
        if (!_inSpawnHub)
        {
            return;
        }

        GameSession.Instance.BeginOverworldRun();
        BuildSceneFromSession();
    }

    private static void ClearNodeChildren(Node3D root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is Node node)
            {
                node.QueueFree();
            }
        }
    }
}
