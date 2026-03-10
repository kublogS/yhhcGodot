using System.Linq;

public static partial class CombatService
{
    public static CombatTurnOutcome CombatTurn(GameState state, CombatTurnRequest request)
    {
        var outcome = new CombatTurnOutcome();
        var player = state.Player;
        if (state.Enemies.Count == 0)
        {
            PushEvent(outcome, CombatEventType.SystemMessage, "Nessun nemico in campo.");
            return outcome;
        }

        var defendActivatedThisTurn = request.ActionType == CombatActionType.Defend;
        player.Defending = defendActivatedThisTurn;
        if (request.ActionType == CombatActionType.Defend)
        {
            PushEvent(outcome, CombatEventType.ActionUsed, "Hai usato Difenditi.", sourceId: PlayerActorId);
            PushEvent(outcome, CombatEventType.DefendActivated, "Ti difendi: difesa +75% per questa fase nemica.", sourceId: PlayerActorId);
        }
        else if (request.ActionType == CombatActionType.Items)
        {
            HandleItems(player, request, outcome);
        }
        else if (request.ActionType == CombatActionType.Flee)
        {
            PushEvent(outcome, CombatEventType.ActionUsed, "Hai tentato la fuga.", sourceId: PlayerActorId);
            if (TryFlee(state))
            {
                PushEvent(outcome, CombatEventType.FleeSucceeded, "Fuga riuscita.", sourceId: PlayerActorId);
                outcome.Fled = true;
                outcome.BattleEnded = true;
                return outcome;
            }

            PushEvent(outcome, CombatEventType.FleeFailed, "Fuga fallita!", sourceId: PlayerActorId);
        }
        else
        {
            HandlePlayerAttack(state, request, outcome);
        }

        HandleDeadEnemies(state, outcome);
        if (state.Enemies.Count == 0 && state.EnemyQueue.Count == 0)
        {
            outcome.BattleEnded = true;
            CleanupDefendState(player, outcome, defendActivatedThisTurn);
            return outcome;
        }

        foreach (var enemy in state.Enemies.ToList())
        {
            if (!enemy.IsAlive || !player.IsAlive)
            {
                continue;
            }

            RunEnemyTurn(enemy, state, outcome);
            if (!player.IsAlive)
            {
                outcome.PlayerDefeated = true;
                PushEvent(outcome, CombatEventType.PlayerKnockedOut, "Sei stato sconfitto...", sourceId: EnemyActorId(enemy, state.Enemies.IndexOf(enemy)), targetId: PlayerActorId);
                break;
            }
        }

        CleanupDefendState(player, outcome, defendActivatedThisTurn);
        HandleDeadEnemies(state, outcome);
        outcome.BattleEnded = state.Enemies.Count == 0 && state.EnemyQueue.Count == 0;
        return outcome;
    }

    private static void CleanupDefendState(CharacterModel player, CombatTurnOutcome outcome, bool defendActivatedThisTurn)
    {
        if (defendActivatedThisTurn)
        {
            PushEvent(outcome, CombatEventType.DefendExpired, "La difesa termina a fine turno.", sourceId: PlayerActorId, targetId: PlayerActorId);
        }

        player.Defending = false;
    }
}
