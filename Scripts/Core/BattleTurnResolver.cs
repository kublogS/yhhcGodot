using System.Linq;

internal static class BattleTurnResolver
{
    public static CombatTurnOutcome Execute(GameState state, CombatTurnRequest request)
    {
        var outcome = new CombatTurnOutcome();
        var player = state.Player;
        if (state.Enemies.Count == 0)
        {
            CombatService.PushEvent(outcome, CombatEventType.SystemMessage, "Nessun nemico in campo.");
            return outcome;
        }

        var defendActivatedThisTurn = request.ActionType == CombatActionType.Defend;
        player.Defending = defendActivatedThisTurn;
        if (BattlePlayerActionResolver.Resolve(state, request, outcome))
        {
            return outcome;
        }

        BattleEnemyLifecycleService.HandleDeadEnemies(state, outcome);
        if (state.Enemies.Count == 0 && state.EnemyQueue.Count == 0)
        {
            outcome.BattleEnded = true;
            CombatService.CleanupDefendState(player, outcome, defendActivatedThisTurn);
            return outcome;
        }

        foreach (var enemy in state.Enemies.ToList())
        {
            if (!enemy.IsAlive || !player.IsAlive)
            {
                continue;
            }

            BattleEnemyTurnResolver.Resolve(enemy, state, outcome);
            if (player.IsAlive)
            {
                continue;
            }

            outcome.PlayerDefeated = true;
            CombatService.PushEvent(
                outcome,
                CombatEventType.PlayerKnockedOut,
                "Sei stato sconfitto...",
                sourceId: CombatService.EnemyActorId(enemy, state.Enemies.IndexOf(enemy)),
                targetId: CombatService.PlayerActorId);
            break;
        }

        CombatService.CleanupDefendState(player, outcome, defendActivatedThisTurn);
        BattleEnemyLifecycleService.HandleDeadEnemies(state, outcome);
        outcome.BattleEnded = state.Enemies.Count == 0 && state.EnemyQueue.Count == 0;
        return outcome;
    }
}
