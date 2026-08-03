using System.Collections.Generic;

public sealed class BattleLogStats
{
    public int Forza { get; set; }
    public int Magia { get; set; }
    public int Difesa { get; set; }
    public int Agilita { get; set; }
    public int Fortuna { get; set; }
    public int Intelligence { get; set; }
    public int Fede { get; set; }
}

public sealed class BattleLogParticipant
{
    public string Name { get; set; } = string.Empty;
    public int MaxHp { get; set; }
    public BattleLogStats Stats { get; set; } = new();
}

public sealed class BattleLogTurnEvent
{
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? SourceId { get; set; }
    public string? TargetId { get; set; }
    public int? Amount { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public sealed class BattleLogTurn
{
    public int TurnNumber { get; set; }
    public string PlayerAction { get; set; } = string.Empty;
    public int PlayerHpAfter { get; set; }
    public List<int> EnemyHpAfter { get; set; } = new();
    public List<BattleLogTurnEvent> Events { get; set; } = new();
}

public sealed class BattleLogRecord
{
    public string BattleId { get; set; } = string.Empty;
    public long StartedAtUnix { get; set; }
    public long EndedAtUnix { get; set; }
    public string Context { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public int PlayerLevel { get; set; }
    public int PlayerMaxHpStart { get; set; }
    public int PlayerHpStart { get; set; }
    public BattleLogStats PlayerStats { get; set; } = new();
    public List<BattleLogParticipant> Enemies { get; set; } = new();
    public List<BattleLogTurn> Turns { get; set; } = new();
    public string Outcome { get; set; } = "InCorso";
    public int PlayerHpEnd { get; set; }
    public int TotalTurns { get; set; }
}
