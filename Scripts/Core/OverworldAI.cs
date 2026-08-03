using System;
using Godot;

public static partial class OverworldAI
{
    public const float WakeDurationSec = 5f;
    public const float StuckTimeSec = 1.5f;
    public const float BehaviorAlertDurationSec = 1.5f;
    public const int SmartIntelligenceMin = 6;
    public const int LowIntelligenceMax = 3;

    public static void TickEnemyRuntime(OverworldEnemyModel enemy, float dt)
    {
        if (enemy.WakeTimer > 0f)
        {
            enemy.WakeTimer = Math.Max(0f, enemy.WakeTimer - dt);
        }
        if (enemy.BehaviorAlertTimer > 0f)
        {
            enemy.BehaviorAlertTimer = Math.Max(0f, enemy.BehaviorAlertTimer - dt);
        }

        if (enemy.SpeechTimer > 0f)
        {
            enemy.SpeechTimer = Math.Max(0f, enemy.SpeechTimer - dt);
            if (enemy.SpeechTimer <= 0f)
            {
                enemy.SpeechText = null;
            }
        }
    }

    public static (float VxPerSec, float VyPerSec, bool Chasing) ComputeVelocity(
        OverworldEnemyModel enemy,
        float playerX,
        float playerY,
        int[,] grid,
        float alertTimer)
    {
        var enemyPos = enemy.Position;
        var dist = MathF.Sqrt((playerX - enemyPos.X) * (playerX - enemyPos.X) + (playerY - enemyPos.Y) * (playerY - enemyPos.Y));
        if (dist <= 0.2f)
        {
            return (0f, 0f, false);
        }

        var visible = HasLineOfSight(grid, enemyPos.X, enemyPos.Y, playerX, playerY);
        if (!DecideChase(enemy, visible, alertTimer))
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

    private static bool DecideChase(OverworldEnemyModel enemy, bool playerVisible, float alertTimer)
    {
        return enemy.BehaviorKind switch
        {
            OverworldBehavior.Pacifista => false,
            OverworldBehavior.Tranquillo => DecideCalmChase(enemy, playerVisible, alertTimer),
            OverworldBehavior.Sospetto => DecideSuspiciousChase(enemy, playerVisible),
            _ => true,
        };
    }

    public static bool ShouldStartEncounterOnContact(OverworldEnemyModel enemy, bool chasing, float alertTimer)
    {
        if (!enemy.Active)
        {
            return false;
        }

        return enemy.BehaviorKind switch
        {
            OverworldBehavior.Pacifista => false,
            OverworldBehavior.Tranquillo => chasing || alertTimer > 0f || enemy.BehaviorAlertTimer > 0f,
            OverworldBehavior.Sospetto => chasing || enemy.Alerted,
            _ => true,
        };
    }

    private static bool DecideCalmChase(OverworldEnemyModel enemy, bool playerVisible, float alertTimer)
    {
        if (playerVisible)
        {
            enemy.BehaviorAlertTimer = MathF.Max(enemy.BehaviorAlertTimer, BehaviorAlertDurationSec);
        }

        return playerVisible && (alertTimer > 0f || enemy.BehaviorAlertTimer > 0f);
    }

    private static bool DecideSuspiciousChase(OverworldEnemyModel enemy, bool playerVisible)
    {
        if (playerVisible)
        {
            enemy.Alerted = true;
        }

        return enemy.Alerted;
    }
}
