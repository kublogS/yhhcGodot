using System.Collections.Generic;

public enum BattleFlowOutcome
{
    ContinueBattle = 0,
    ReturnToExplore,
    OpenReward,
    GoToMainMenu,
}

public sealed class BattleScreenState
{
    public List<string> MoveLabels { get; init; } = new();
}

public sealed class BattleFlowResult
{
    public List<string> LogLines { get; } = new();
    public List<CombatEventEntry> Events { get; } = new();
    public BattleFlowOutcome Outcome { get; set; } = BattleFlowOutcome.ContinueBattle;
}

public static class BattleFlowOutcomeRouting
{
    public static SceneRoute ToSceneRoute(BattleFlowOutcome outcome)
    {
        return outcome switch
        {
            BattleFlowOutcome.ReturnToExplore => SceneRoute.Explore,
            BattleFlowOutcome.OpenReward => SceneRoute.Reward,
            BattleFlowOutcome.GoToMainMenu => SceneRoute.MainMenu,
            _ => SceneRoute.None,
        };
    }
}
