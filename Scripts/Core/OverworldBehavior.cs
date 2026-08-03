using System;

public enum OverworldBehavior
{
    Pacifista = 0,
    Tranquillo = 1,
    Sospetto = 2,
    Ansioso = 3,
}

public static class OverworldBehaviorParser
{
    public static OverworldBehavior Parse(string? value)
    {
        return Enum.TryParse<OverworldBehavior>(value, ignoreCase: true, out var parsed)
            ? parsed
            : OverworldBehavior.Sospetto;
    }
}
