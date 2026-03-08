using System;

public static partial class OverworldAI
{
    public const float WakeDurationSec = 5f;
    public const float StuckTimeSec = 1.5f;
    public const int SmartIntelligenceMin = 6;
    public const int LowIntelligenceMax = 3;

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
    }

    public static (float VxPerSec, float VyPerSec, bool Chasing) ComputeVelocity(
        OverworldEnemyModel enemy,
        float playerX,
        float playerY,
        int[,] grid,
        float alertTimer)
    {
        var dist = MathF.Sqrt((playerX - enemy.X) * (playerX - enemy.X) + (playerY - enemy.Y) * (playerY - enemy.Y));
        if (dist <= 0.2f)
        {
            return (0f, 0f, false);
        }

        var visible = HasLineOfSight(grid, enemy.X, enemy.Y, playerX, playerY);
        if (!DecideChase(enemy, visible, alertTimer))
        {
            return (0f, 0f, false);
        }

        var sx = (int)MathF.Floor(enemy.X);
        var sy = (int)MathF.Floor(enemy.Y);
        var tx = (int)MathF.Floor(playerX);
        var ty = (int)MathF.Floor(playerY);

        var (ox, oy) = enemy.IntelligenceY >= SmartIntelligenceMin || enemy.WakeTimer > 0f
            ? BfsNextStep(grid, sx, sy, tx, ty)
            : GreedyStep(grid, sx, sy, tx, ty);

        if (ox == 0 && oy == 0 && enemy.IntelligenceY <= LowIntelligenceMax)
        {
            return (0f, 0f, true);
        }

        var speed = Math.Max(0.2f, enemy.SpeedX);
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
        return enemy.Behavior switch
        {
            "Pacifista" => false,
            "Tranquillo" => playerVisible && alertTimer > 0f,
            "Sospetto" => enemy.Alerted || playerVisible,
            _ => true,
        };
    }
}
