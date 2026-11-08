using System;

public enum OverworldBehavior
{
    Normale = 0,
    Allerta = 1,
    Pacifico = 2,
    Paranoico = 3,
}

public static class OverworldBehaviorParser
{
    public static OverworldBehavior Parse(string? value)
    {
        return Enum.TryParse<OverworldBehavior>(value, ignoreCase: true, out var parsed)
            ? parsed
            : OverworldBehavior.Normale;
    }
}
