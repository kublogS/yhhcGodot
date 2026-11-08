using System.Collections.Generic;

// GOAP-style decision-making, not a rule table ("if HP < X then Y"): the
// world state is "how much can I hurt the player, given what I currently
// believe about their type" (see EnemyPlayerIntel - never the true type),
// the goal is minimizing the player's HP, and each castable move plus a
// plain attack is an action with a precondition (can it currently be
// afforded) and a simulated effect (expected damage toward the goal,
// averaged over several rehearsals to smooth out misses/crits). The planner
// only ever needs to reason one action deep: the game commits exactly one
// action per turn, so there is no sequence to chain within a single
// decision - "planning" here is picking the single best-scoring legal action.
internal static class EnemyCombatAi
{
    private const int SimulationSamples = 20;
    private const float NullifiedPenalty = -1_000_000f;

    private readonly record struct GoapAction(MoveModel? Move);

    public static MoveModel? ChooseBestMove(CharacterModel enemy, CharacterModel player, GameState state)
    {
        var believedType = state.PlayerIntel.GuessDefenderType();
        var actions = BuildActions(enemy);

        // Common-random-numbers: every action's rehearsal draws from the same
        // scratch stream, seeded from the battle's own RNG state (read, not
        // consumed) so the "thinking" is exploratory in effect but fully
        // reproducible for a given seed - two runs of the same battle still
        // play out identically.
        var scratch = new GameRng(unchecked((int)state.Rng.State));

        GoapAction best = default;
        var bestScore = float.NegativeInfinity;
        foreach (var action in actions)
        {
            var score = EstimateEffect(enemy, player, action, believedType, scratch);
            if (score > bestScore)
            {
                bestScore = score;
                best = action;
            }
        }

        return best.Move;
    }

    private static List<GoapAction> BuildActions(CharacterModel enemy)
    {
        var actions = new List<GoapAction> { new(null) };
        foreach (var move in enemy.Moves)
        {
            // Precondition: the enemy can currently afford this action.
            if (move is not null && CombatService.CanCastMove(enemy, move))
            {
                actions.Add(new GoapAction(move));
            }
        }

        return actions;
    }

    private static float EstimateEffect(
        CharacterModel enemy,
        CharacterModel player,
        GoapAction action,
        string? believedDefenderType,
        GameRng scratch)
    {
        var total = 0f;
        for (var i = 0; i < SimulationSamples; i++)
        {
            if (action.Move is null)
            {
                total += CombatService.RollAttack(enemy, player, scratch).RawDamage;
                continue;
            }

            var (damage, tags) = CombatService.ComputeMoveDamage(
                enemy,
                player,
                action.Move,
                scratch,
                believedDefenderTypeOverride: believedDefenderType);
            total += tags.Nullified ? NullifiedPenalty : damage;
        }

        return total / SimulationSamples;
    }
}
