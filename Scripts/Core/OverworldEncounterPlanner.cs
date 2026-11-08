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

        foreach (var reinforcement in SelectReinforcements(primary, overworldEnemies, reinforcementRadius))
        {
            plan.ConsumedEnemies.Add(reinforcement);
            if (plan.ActiveEnemies.Count < 3)
            {
                plan.ActiveEnemies.Add(reinforcement.ToBattleCharacter(rng));
                continue;
            }

            plan.QueuedEnemies.Add(reinforcement.ToBattleCharacter(rng));
        }

        return plan;
    }

    // Split out from Build() so the "who counts as reinforcement" rule can be
    // unit-tested without also exercising ToBattleCharacter (which reaches
    // into the Database autoload and can't run outside a live Godot engine).
    public static List<OverworldEnemyModel> SelectReinforcements(
        OverworldEnemyModel primary,
        IReadOnlyList<OverworldEnemyModel> overworldEnemies,
        float reinforcementRadius)
    {
        // Only Normale/Allerta enemies pile into a nearby fight as
        // reinforcement - a Pacifico never fights, and a Paranoico is busy
        // following its own search/patrol loop rather than reacting to a
        // fight it happens to be near.
        return overworldEnemies
            .Where(enemy => enemy.Active && !ReferenceEquals(enemy, primary))
            .Where(enemy => enemy.BehaviorKind is OverworldBehavior.Normale or OverworldBehavior.Allerta)
            .Select(enemy => (Enemy: enemy, Distance: enemy.XYDistanceTo(primary.Position)))
            .Where(candidate => candidate.Distance <= reinforcementRadius)
            .OrderBy(candidate => candidate.Distance)
            .Select(candidate => candidate.Enemy)
            .ToList();
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
