using System.Collections.Generic;

public static partial class CombatService
{
    internal const string PlayerActorId = "player";

    internal static void PushEvent(
        CombatTurnOutcome outcome,
        CombatEventType type,
        string message,
        string? sourceId = null,
        string? targetId = null,
        int? sourceSlot = null,
        int? targetSlot = null,
        int? amount = null,
        Dictionary<string, string>? metadata = null)
    {
        outcome.LogLines.Add(message);
        outcome.Events.Add(new CombatEventEntry
        {
            EventType = type,
            Message = message,
            SourceId = sourceId,
            TargetId = targetId,
            SourceSlot = sourceSlot,
            TargetSlot = targetSlot,
            Amount = amount,
            Metadata = metadata ?? new Dictionary<string, string>(),
        });
    }

    internal static string EnemyActorId(CharacterModel enemy, int slot)
    {
        if (!string.IsNullOrWhiteSpace(enemy.BattleActorId))
        {
            return enemy.BattleActorId;
        }

        return $"enemy:{slot}:{enemy.Name}";
    }
}
