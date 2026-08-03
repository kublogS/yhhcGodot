using System;

public enum RunLocation
{
    Indice = 0,
    Overworld = 1,
}

public sealed class RunStateContract
{
    public bool IsRunActive { get; init; }
    public RunLocation Location { get; init; } = RunLocation.Indice;
    public int ProceduralSeed { get; init; }
    public int ProceduralFloor { get; init; }
    public int ProceduralMaxFloors { get; init; } = 20;

    public bool HasOverworldRuntime => IsRunActive && Location == RunLocation.Overworld && ProceduralSeed > 0;

    public static RunLocation ParseLocation(string? value, bool fallbackHasOverworld)
    {
        if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<RunLocation>(value, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return fallbackHasOverworld ? RunLocation.Overworld : RunLocation.Indice;
    }
}
