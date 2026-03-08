using System;
using System.Collections.Generic;
using System.Text.Json;

public static partial class Moves
{
    private static readonly Dictionary<string, string> MoveTypeAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["psichico"] = "psicologico",
        ["psicologico"] = "psicologico",
        ["psichico/psicologico"] = "psicologico",
        ["psicologico/psichico"] = "psicologico",
    };

    private static string? NormalizeString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string? NormalizeCostCategory(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var key = value.Trim().ToLowerInvariant();
        return key switch
        {
            "mana" or "mn" => "mana",
            "vita" or "hp" or "life" => "vita",
            "esperienza" or "exp" or "xp" => "exp",
            _ => key,
        };
    }

    private static string? NormalizeMoveType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var key = value.Trim().ToLowerInvariant();
        return MoveTypeAliases.GetValueOrDefault(key, key);
    }

    private static string? GetString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.GetString();
    }

    private static int? TryGetInt(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return TryGetInt(value);
    }

    private static int? TryGetInt(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var n))
        {
            return n;
        }

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static bool TryGetBool(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => value.TryGetInt32(out var n) && n != 0,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var b) && b,
            _ => false,
        };
    }
}
