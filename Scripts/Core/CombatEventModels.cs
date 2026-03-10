using System.Collections.Generic;

public enum CombatEventType
{
    ActionUsed,
    DefendActivated,
    DefendExpired,
    FleeSucceeded,
    FleeFailed,
    DamageDealt,
    DamageMitigated,
    ItemUsed,
    Healed,
    EnemyKnockedOut,
    PlayerKnockedOut,
    EnemyPromotedFromQueue,
    NonDamagingAction,
    SystemMessage,
}

public sealed class CombatEventEntry
{
    public CombatEventType EventType { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? SourceId { get; init; }
    public string? TargetId { get; init; }
    public int? SourceSlot { get; init; }
    public int? TargetSlot { get; init; }
    public int? Amount { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}
