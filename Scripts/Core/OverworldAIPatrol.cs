using System;
using System.Collections.Generic;
using Godot;

public static partial class OverworldAI
{
    public const float ParanoidAskRadius = 8f;
    public const float ParanoidArrivalDistance = 0.6f;
    public const float ParanoidLeadForgetSec = 12f;

    private static (float VxPerSec, float VyPerSec, bool Chasing) ComputeParanoidVelocity(OverworldEnemyModel enemy, DungeonData dungeon)
    {
        var target = ResolveParanoidTarget(enemy, dungeon);
        if (target is not Vector2 destination)
        {
            return (0f, 0f, false);
        }

        var enemyPos = enemy.Position;
        if (enemyPos.DistanceTo(destination) <= ParanoidArrivalDistance)
        {
            HandleParanoidArrival(enemy);
            return (0f, 0f, false);
        }

        var grid = dungeon.Grid;
        var startGrid = DungeonMetrics.WorldToGrid(new Vector3(enemyPos.X, 0f, enemyPos.Y), DungeonMetrics.TileSize);
        var targetGrid = DungeonMetrics.WorldToGrid(new Vector3(destination.X, 0f, destination.Y), DungeonMetrics.TileSize);
        var (ox, oy) = enemy.Intelligence >= SmartIntelligenceMin
            ? BfsNextStep(grid, startGrid.X, startGrid.Y, targetGrid.X, targetGrid.Y)
            : GreedyStep(grid, startGrid.X, startGrid.Y, targetGrid.X, targetGrid.Y);

        if (ox == 0 && oy == 0)
        {
            // Can't path any closer (unreachable room, stuck, ...) - treat it
            // like arrival instead of getting permanently wedged.
            HandleParanoidArrival(enemy);
            return (0f, 0f, false);
        }

        var speed = Math.Max(0.2f, enemy.Speed) * DungeonMetrics.TileSize;
        return (ox * speed, oy * speed, false);
    }

    private static Vector2? ResolveParanoidTarget(OverworldEnemyModel enemy, DungeonData dungeon)
    {
        if (enemy.LastKnownPlayerPosition is Vector2 lead && enemy.LastKnownPlayerAgeSec <= ParanoidLeadForgetSec)
        {
            return lead;
        }

        enemy.LastKnownPlayerPosition = null;
        enemy.LastKnownPlayerAgeSec = float.PositiveInfinity;

        if (enemy.PatrolTargetRoomId is int roomId && dungeon.RoomBounds.TryGetValue(roomId, out var bounds))
        {
            return RoomCenterWorld(bounds);
        }

        return ChooseNextPatrolRoom(enemy, dungeon);
    }

    private static Vector2? ChooseNextPatrolRoom(OverworldEnemyModel enemy, DungeonData dungeon)
    {
        if (dungeon.RoomBounds.Count == 0)
        {
            return null;
        }

        if (enemy.VisitedRoomIds.Count >= dungeon.RoomBounds.Count)
        {
            enemy.VisitedRoomIds.Clear();
        }

        var enemyPos = enemy.Position;
        int? bestId = null;
        var bestDistSq = float.MaxValue;
        foreach (var pair in dungeon.RoomBounds)
        {
            if (enemy.VisitedRoomIds.Contains(pair.Key))
            {
                continue;
            }

            var distSq = enemyPos.DistanceSquaredTo(RoomCenterWorld(pair.Value));
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestId = pair.Key;
            }
        }

        if (bestId is not int chosen)
        {
            return null;
        }

        enemy.PatrolTargetRoomId = chosen;
        return RoomCenterWorld(dungeon.RoomBounds[chosen]);
    }

    private static void HandleParanoidArrival(OverworldEnemyModel enemy)
    {
        if (enemy.LastKnownPlayerPosition is not null)
        {
            // Followed a lead here and the player wasn't actually found -
            // drop it and go back to patrolling.
            enemy.LastKnownPlayerPosition = null;
            enemy.LastKnownPlayerAgeSec = float.PositiveInfinity;
            return;
        }

        if (enemy.PatrolTargetRoomId is int roomId)
        {
            enemy.VisitedRoomIds.Add(roomId);
            enemy.PatrolTargetRoomId = null;
        }
    }

    private static Vector2 RoomCenterWorld(Rect2I bounds)
    {
        var centerGrid = bounds.Position + (bounds.Size / 2);
        var world = DungeonMetrics.GridToWorld(centerGrid.X, centerGrid.Y);
        return new Vector2(world.X, world.Z);
    }

    // Called periodically for every active Paranoico (see
    // OverworldEnemyRuntimeCoordinator): asks nearby allies - of any
    // behavior, Pacifico included - whether they've personally seen the
    // player, and adopts the freshest sighting as its new lead. The
    // Paranoico itself never learns the player's true position directly; it
    // only ever repeats what someone else claims to have seen, so a stale or
    // wrong lead is possible and it will eventually give up and resume
    // patrolling if the lead doesn't pan out.
    public static void ParanoidAskAround(OverworldEnemyModel paranoid, IReadOnlyList<OverworldEnemyModel> allies)
    {
        Vector2? bestPosition = null;
        var bestAge = float.PositiveInfinity;
        foreach (var ally in allies)
        {
            if (ReferenceEquals(ally, paranoid) || !ally.Active)
            {
                continue;
            }

            if (ally.LastKnownPlayerPosition is not Vector2 sighting || ally.LastKnownPlayerAgeSec > ParanoidLeadForgetSec)
            {
                continue;
            }

            if (paranoid.Position.DistanceSquaredTo(ally.Position) > ParanoidAskRadius * ParanoidAskRadius)
            {
                continue;
            }

            if (ally.LastKnownPlayerAgeSec < bestAge)
            {
                bestAge = ally.LastKnownPlayerAgeSec;
                bestPosition = sighting;
            }
        }

        if (bestPosition is not Vector2 lead)
        {
            return;
        }

        var currentIsFresherOrEqual = paranoid.LastKnownPlayerPosition is not null && paranoid.LastKnownPlayerAgeSec <= bestAge;
        if (currentIsFresherOrEqual)
        {
            return;
        }

        paranoid.LastKnownPlayerPosition = lead;
        paranoid.LastKnownPlayerAgeSec = bestAge;
    }
}
