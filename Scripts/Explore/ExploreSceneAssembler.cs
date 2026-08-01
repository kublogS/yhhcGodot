using System.Collections.Generic;
using Godot;

public sealed class ExploreSceneAssembler
{
    public SpawnHubLayout BuildSpawnHub(
        GameSession session,
        Node3D dungeonRoot,
        Node3D enemiesRoot,
        PlayerController player,
        ref Node3D? manualPanel)
    {
        session.ReturnToSpawnHub();
        ClearNodeChildren(enemiesRoot);
        manualPanel?.QueueFree();

        var layout = SpawnHubBuilder.Build(dungeonRoot);
        player.ClearNavigation();
        player.GlobalPosition = layout.PlayerSpawn;
        player.Rotation = Vector3.Zero;
        manualPanel = ManualPosterBuilder.Build(
            dungeonRoot,
            layout.PosterAnchor,
            layout.PosterNormal,
            scale: 1.45f);
        return layout;
    }

    public List<EnemyAgent> BuildDungeon(
        GameSession session,
        Node3D dungeonRoot,
        Node3D enemiesRoot,
        PlayerController player,
        ref Node3D? manualPanel,
        OverworldEnemyRuntimeCoordinator? runtimeCoordinator = null)
    {
        var dungeon = session.CurrentDungeon!;
        if (!dungeon.LayoutTuned)
        {
            DungeonLayoutTuner.EnsureComfortablePassages(dungeon);
            dungeon.LayoutTuned = true;
        }

        DungeonLayoutTuner.EnsureWallEnvelope(dungeon);
        DungeonBuilder.Build(dungeonRoot, dungeon);
        player.ConfigureDungeonNavigation(dungeon, DungeonMetrics.TileSize);
        player.GlobalPosition = session.PlayerWorldPosition == Vector3.Zero
            ? dungeon.PlayerSpawn + new Vector3(0f, 1.2f, 0f)
            : session.PlayerWorldPosition;
        player.Rotation = new Vector3(0f, session.PlayerYawRadians, 0f);

        ClearNodeChildren(enemiesRoot);
        var enemyAgents = new List<EnemyAgent>();
        foreach (var enemy in session.OverworldEnemies)
        {
            var agent = new EnemyAgent();
            enemiesRoot.AddChild(agent);
            agent.Setup(enemy, player, dungeon, runtimeCoordinator);
            enemyAgents.Add(agent);
        }

        manualPanel?.QueueFree();
        var (anchor, normal) = FindDungeonManualAnchor(dungeon);
        manualPanel = ManualPosterBuilder.Build(dungeonRoot, anchor, normal);
        return enemyAgents;
    }

    private static (Vector3 Position, Vector3 Normal) FindDungeonManualAnchor(DungeonData dungeon)
    {
        var tileSize = DungeonMetrics.TileSize;
        var spawnTile = DungeonGenerator.WorldToGrid(dungeon.PlayerSpawn, tileSize);
        var spawnWorld = DungeonGenerator.GridToWorld(spawnTile.X, spawnTile.Y, tileSize);
        var bestDistance = float.MaxValue;
        var bestPosition = spawnWorld + new Vector3(0f, 2.0f, -2.0f);
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

                var probe = DungeonGenerator.WorldToGrid(wallWorld + (normal * tileSize), tileSize);
                if (!dungeon.CanTraverse(probe.X, probe.Y))
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
                bestPosition = wallWorld + (normal * (tileSize * 0.52f)) + new Vector3(0f, 2.0f, 0f);
            }
        }

        return (bestPosition, bestNormal);
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
