using System.Collections.Generic;

public sealed class BattleTargetView
{
    public string Label { get; init; } = string.Empty;
    public bool Visible { get; init; }
    public bool Selected { get; init; }
}

public sealed class BattleScreenState
{
    public List<string> MoveLabels { get; init; } = new();
    public List<BattleTargetView> Targets { get; init; } = new();
}

public sealed class BattleFlowResult
{
    public List<string> LogLines { get; } = new();
    public List<CombatEventEntry> Events { get; } = new();
    public SceneRoute Route { get; set; } = SceneRoute.None;
}
