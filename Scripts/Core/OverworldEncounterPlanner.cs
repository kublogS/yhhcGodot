using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public sealed class OverworldEncounterPlan
{
    public List<CharacterModel> ActiveEnemies { get; } = new();
    public List<CharacterModel> QueuedEnemies { get; } = new();
    public List<OverworldEnemyModel> ConsumedEnemies { get; } = new();
}

public static class OverworldEncounterPlanner
{
    public static OverworldEncounterPlan Build(
        OverworldEnemyModel primary,
        IReadOnlyList<OverworldEnemyModel> overworldEnemies,
        GameRng rng,
        float reinforcementRadius)
    {
        var plan = new OverworldEncounterPlan();
        plan.ActiveEnemies.Add(primary.ToBattleCharacter(rng));
        plan.ConsumedEnemies.Add(primary);

        var nearby = overworldEnemies
            .Where(enemy => enemy.Active && !ReferenceEquals(enemy, primary))
            .Select(enemy => (Enemy: enemy, Distance: enemy.XYDistanceTo(primary.Position)))
            .Where(candidate => candidate.Distance <= reinforcementRadius)
            .OrderBy(candidate => candidate.Distance);

        foreach (var candidate in nearby)
        {
            plan.ConsumedEnemies.Add(candidate.Enemy);
            if (plan.ActiveEnemies.Count < 3)
            {
                plan.ActiveEnemies.Add(candidate.Enemy.ToBattleCharacter(rng));
                continue;
            }

            plan.QueuedEnemies.Add(candidate.Enemy.ToBattleCharacter(rng));
        }

        return plan;
    }
}

public static class OverworldEnemyDistanceExtensions
{
    public static float XYDistanceTo(this OverworldEnemyModel enemy, Vector2 target)
    {
        var position = enemy.Position;
        var dx = position.X - target.X;
        var dy = position.Y - target.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }
}
