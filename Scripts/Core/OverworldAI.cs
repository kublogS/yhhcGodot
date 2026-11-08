using System;
using Godot;

public static partial class OverworldAI
{
    public const float WakeDurationSec = 5f;
    public const float StuckTimeSec = 1.5f;
    public const int SmartIntelligenceMin = 6;
    public const int LowIntelligenceMax = 3;
    public const float SightMemoryForgetSec = 20f;

    public static void TickEnemyRuntime(OverworldEnemyModel enemy, float dt)
    {
        if (enemy.WakeTimer > 0f)
        {
            enemy.WakeTimer = Math.Max(0f, enemy.WakeTimer - dt);
        }

        if (enemy.SpeechTimer > 0f)
        {
            enemy.SpeechTimer = Math.Max(0f, enemy.SpeechTimer - dt);
            if (enemy.SpeechTimer <= 0f)
            {
                enemy.SpeechText = null;
            }
        }

        if (float.IsFinite(enemy.LastKnownPlayerAgeSec))
        {
            enemy.LastKnownPlayerAgeSec += dt;
        }
    }

    public static (float VxPerSec, float VyPerSec, bool Chasing) ComputeVelocity(
        OverworldEnemyModel enemy,
        float playerX,
        float playerY,
        DungeonData dungeon,
        float alertTimer)
    {
        var grid = dungeon.Grid;
        var enemyPos = enemy.Position;
        var visible = HasLineOfSight(grid, enemyPos.X, enemyPos.Y, playerX, playerY);
        if (visible)
        {
            enemy.LastKnownPlayerPosition = new Vector2(playerX, playerY);
            enemy.LastKnownPlayerAgeSec = 0f;
        }
        else if (enemy.LastKnownPlayerAgeSec > SightMemoryForgetSec)
        {
            enemy.LastKnownPlayerPosition = null;
        }

        // Paranoico never chases what it directly sees - it hunts by
        // patrolling rooms and by what other enemies tell it (see
        // OverworldAIPatrol.cs), so it takes a completely separate movement
        // path instead of the "chase the player's live position" logic below.
        if (enemy.BehaviorKind == OverworldBehavior.Paranoico)
        {
            return ComputeParanoidVelocity(enemy, dungeon);
        }

        var dist = MathF.Sqrt(((playerX - enemyPos.X) * (playerX - enemyPos.X)) + ((playerY - enemyPos.Y) * (playerY - enemyPos.Y)));
        if (dist <= 0.2f)
        {
            return (0f, 0f, false);
        }

        if (!DecideChase(enemy, visible))
        {
            return (0f, 0f, false);
        }

        var enemyGrid = DungeonMetrics.WorldToGrid(new Vector3(enemyPos.X, 0f, enemyPos.Y), DungeonMetrics.TileSize);
        var playerGrid = DungeonMetrics.WorldToGrid(new Vector3(playerX, 0f, playerY), DungeonMetrics.TileSize);
        var sx = enemyGrid.X;
        var sy = enemyGrid.Y;
        var tx = playerGrid.X;
        var ty = playerGrid.Y;

        var (ox, oy) = enemy.Intelligence >= SmartIntelligenceMin || enemy.WakeTimer > 0f
            ? BfsNextStep(grid, sx, sy, tx, ty)
            : GreedyStep(grid, sx, sy, tx, ty);

        if (ox == 0 && oy == 0 && enemy.Intelligence <= LowIntelligenceMax)
        {
            return (0f, 0f, true);
        }

        var speed = Math.Max(0.2f, enemy.Speed) * DungeonMetrics.TileSize;
        return (ox * speed, oy * speed, true);
    }

    public static void MarkMoveResult(OverworldEnemyModel enemy, bool moved, float dt)
    {
        if (moved)
        {
            enemy.StuckTimer = 0f;
            enemy.IsStuck = false;
            return;
        }

        enemy.StuckTimer += dt;
        if (enemy.StuckTimer >= StuckTimeSec)
        {
            enemy.IsStuck = true;
        }
    }

    // Whether the enemy actively chases the player's live, currently-visible
    // position via the overworld's own line of sight (not via reinforcement
    // or a shared/patrol lead - those are separate triggers).
    private static bool DecideChase(OverworldEnemyModel enemy, bool playerVisible)
    {
        return enemy.BehaviorKind switch
        {
            OverworldBehavior.Allerta => playerVisible,
            _ => false,
        };
    }

    // Whether touching/being touched by the player should start a battle.
    // Normale never hunts from a distance (see DecideChase), but still
    // fights if the player walks right into it - it only gets pulled into a
    // fight it merely *witnesses* from afar via the separate reinforcement
    // path in OverworldEncounterPlanner, not through this direct-contact check.
    public static bool ShouldStartEncounterOnContact(OverworldEnemyModel enemy, bool chasing, float alertTimer)
    {
        if (!enemy.Active)
        {
            return false;
        }

        return enemy.BehaviorKind switch
        {
            OverworldBehavior.Pacifico => false,
            _ => true,
        };
    }
}
