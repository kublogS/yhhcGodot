using System.Collections.Generic;

public static class BattleLogEventFactory
{
    public static List<CombatEventEntry> FromLogLines(IReadOnlyList<string> lines)
    {
        var events = new List<CombatEventEntry>(lines.Count);
        foreach (var line in lines)
        {
            events.Add(new CombatEventEntry
            {
                EventType = CombatEventType.SystemMessage,
                Message = line,
            });
        }

        return events;
    }
}
