using System;
using System.Collections.Generic;

public static partial class OverworldAI
{
    public const float WakeScanRadius = 2.5f;

    private static readonly string[] WakeInsultLines =
    {
        "Muoviti!",
        "Guarda la mappa!",
        "Non sbattere sul muro!",
        "Usa il cervello!",
    };

    public static void MaybeWakeStuckAlly(
        OverworldEnemyModel enemy,
        IReadOnlyList<OverworldEnemyModel> allies,
        GameRng rng)
    {
        if (enemy.Intelligence < SmartIntelligenceMin || enemy.SpeechTimer > 0f)
        {
            return;
        }

        foreach (var ally in allies)
        {
            if (ReferenceEquals(ally, enemy) || !ally.Active)
            {
                continue;
            }

            if (!ally.IsStuck || ally.Intelligence > LowIntelligenceMax)
            {
                continue;
            }

            var dist = XYDistance(enemy, ally);
            if (dist > WakeScanRadius)
            {
                continue;
            }

            ally.IsStuck = false;
            ally.StuckTimer = 0f;
            ally.WakeTimer = WakeDurationSec;
            ally.SpeechText = RandomWakeLine(rng);
            ally.SpeechTimer = WakeDurationSec;

            enemy.WakeTimer = WakeDurationSec;
            enemy.SpeechText = RandomWakeLine(rng);
            enemy.SpeechTimer = WakeDurationSec;
            return;
        }
    }

    private static float XYDistance(OverworldEnemyModel a, OverworldEnemyModel b)
    {
        var dx = a.Position.X - b.Position.X;
        var dy = a.Position.Y - b.Position.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }

    private static string RandomWakeLine(GameRng rng)
    {
        if (WakeInsultLines.Length == 0)
        {
            return "Muoviti!";
        }

        var index = rng.NextInt(0, WakeInsultLines.Length - 1);
        return WakeInsultLines[index];
    }
}
