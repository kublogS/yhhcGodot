public static partial class CombatService
{
    public static CombatTurnOutcome CombatTurn(GameState state, CombatTurnRequest request)
    {
        return BattleTurnResolver.Execute(state, request);
    }

    internal static void CleanupDefendState(CharacterModel player, CombatTurnOutcome outcome, bool defendActivatedThisTurn)
    {
        if (defendActivatedThisTurn)
        {
            PushEvent(outcome, CombatEventType.DefendExpired, "La difesa termina a fine turno.", sourceId: PlayerActorId, targetId: PlayerActorId);
        }

        player.Defending = false;
    }
}
