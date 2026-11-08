public interface IBattleAnalyticsSink
{
    void BeginBattle(GameState state, string context);
    void RecordTurn(GameState state, CombatTurnRequest request, CombatTurnOutcome outcome);
    void EndBattle(GameState state, string outcome);
}

public sealed class NullBattleAnalyticsSink : IBattleAnalyticsSink
{
    public static readonly NullBattleAnalyticsSink Instance = new();

    public void BeginBattle(GameState state, string context)
    {
    }

    public void RecordTurn(GameState state, CombatTurnRequest request, CombatTurnOutcome outcome)
    {
    }

    public void EndBattle(GameState state, string outcome)
    {
    }
}

public sealed class BattleAnalyticsLogSink : IBattleAnalyticsSink
{
    public static readonly BattleAnalyticsLogSink Instance = new();

    public void BeginBattle(GameState state, string context)
    {
        BattleAnalyticsLog.BeginBattle(state, context);
    }

    public void RecordTurn(GameState state, CombatTurnRequest request, CombatTurnOutcome outcome)
    {
        BattleAnalyticsLog.RecordTurn(state, request, outcome);
    }

    public void EndBattle(GameState state, string outcome)
    {
        BattleAnalyticsLog.EndBattle(state, outcome);
    }
}
