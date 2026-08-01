using System;

public static class CharacterStatKeys
{
    public const string Strength = "forza";
    public const string Magic = "magia";
    public const string Defense = "difesa";
    public const string Faith = "fede";
    public const string Intelligence = "intelligence";

    public static string Normalize(string stat)
    {
        return (stat ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "intelligenza" => Intelligence,
            _ => (stat ?? string.Empty).Trim().ToLowerInvariant(),
        };
    }
}
