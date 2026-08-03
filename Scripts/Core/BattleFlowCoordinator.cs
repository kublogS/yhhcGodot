using System.Collections.Generic;

public sealed class BattleFlowCoordinator
{
    private readonly IBattleSessionContext _session;
    private readonly IBattleAnalyticsSink _analytics;

    public BattleFlowCoordinator(IBattleSessionContext session, IBattleAnalyticsSink? analytics = null)
    {
        _session = session;
        _analytics = analytics ?? NullBattleAnalyticsSink.Instance;
    }

    public BattleFlowResult ExecuteTurn(CombatTurnRequest request)
    {
        var result = new BattleFlowResult();
        var state = _session.State;
        if (state is null)
        {
            AppendSystemEvent(result, "Sessione non valida: ritorno al menu principale.");
            result.Outcome = BattleFlowOutcome.GoToMainMenu;
            return result;
        }

        var outcome = CombatService.CombatTurn(state, request);
        result.LogLines.AddRange(outcome.LogLines);
        result.Events.AddRange(outcome.Events);
        _analytics.RecordTurn(state, request, outcome);

        if (outcome.PlayerDefeated)
        {
            AppendSystemEvent(result, "Game Over.");
            _analytics.EndBattle(state, "Sconfitta");
            result.Outcome = BattleFlowOutcome.GoToMainMenu;
            return result;
        }

        if (outcome.Fled)
        {
            _analytics.EndBattle(state, "Fuga");
            result.Outcome = BattleFlowOutcome.ReturnToExplore;
            return result;
        }

        if (outcome.BattleEnded)
        {
            var rewards = _session.FinalizeBattleRewards();
            AppendSystemEvent(result, $"Ricompense: Soli {rewards.Soli}, Oggetti {rewards.Items}, Mosse {rewards.Tokens}");
            _analytics.EndBattle(state, "Vittoria");
            result.Outcome = BattleFlowOutcome.OpenReward;
            return result;
        }

        return result;
    }

    public BattleScreenState? BuildScreenState(int selectedTarget)
    {
        var state = _session.State;
        if (state is null)
        {
            return null;
        }

        while (state.Player.Moves.Count < 5)
        {
            state.Player.Moves.Add(null);
        }

        var moves = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            var move = state.Player.Moves[i] ?? Moves.BasicAttackMove();
            moves.Add($"{i + 1}. {move.Name}");
        }

        return new BattleScreenState { MoveLabels = moves };
    }

    private static void AppendSystemEvent(BattleFlowResult result, string message)
    {
        result.LogLines.Add(message);
        result.Events.Add(new CombatEventEntry
        {
            EventType = CombatEventType.SystemMessage,
            Message = message,
        });
    }
}
