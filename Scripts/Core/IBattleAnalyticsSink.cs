public interface IBattleAnalyticsSink
{
    void RecordTurn(GameState state, CombatTurnRequest request, CombatTurnOutcome outcome);
    void EndBattle(GameState state, string outcome);
}

public sealed class NullBattleAnalyticsSink : IBattleAnalyticsSink
{
    public static readonly NullBattleAnalyticsSink Instance = new();

    public void RecordTurn(GameState state, CombatTurnRequest request, CombatTurnOutcome outcome)
    {
    }

    public void EndBattle(GameState state, string outcome)
    {
    }
}

public sealed class BattleAnalyticsLogSink : IBattleAnalyticsSink
{
    public void RecordTurn(GameState state, CombatTurnRequest request, CombatTurnOutcome outcome)
    {
        BattleAnalyticsLog.RecordTurn(state, request, outcome);
    }

    public void EndBattle(GameState state, string outcome)
    {
        BattleAnalyticsLog.EndBattle(state, outcome);
    }
}
