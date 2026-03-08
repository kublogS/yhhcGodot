using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public static partial class TypeSystem
{
    private static readonly Dictionary<string, string> TypeAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["psichico"] = "Psicologico",
        ["psicologico"] = "Psicologico",
        ["psichico/psicologico"] = "Psicologico",
        ["psicologico/psichico"] = "Psicologico",
    };

    private static TypeSystemConfig? _config;

    public static void SetConfig(TypeSystemConfig config)
    {
        _config = config;
    }

    public static TypeSystemConfig GetConfig()
    {
        _config ??= BuildConfig("{}");
        return _config;
    }

    public static TypeSystemConfig BuildConfig(string json)
    {
        var cfg = new TypeSystemConfig();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        LoadTypes(root, cfg);
        cfg.TypeLookup = BuildTypeLookup(cfg.Types);
        ApplyTypeAliases(cfg.TypeLookup, cfg.Types);

        LoadFamilies(root, cfg);
        LoadWeakness(root, cfg);
        LoadFriendGroups(root, cfg);
        LoadStatScaling(root, cfg);
        LoadScaling(root, cfg);
        LoadModifiers(root, cfg);
        return cfg;
    }

    private static void LoadTypes(JsonElement root, TypeSystemConfig cfg)
    {
        if (!root.TryGetProperty("types", out var typesNode) || typesNode.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var type in typesNode.EnumerateArray())
        {
            var value = type.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                cfg.Types.Add(value!);
            }
        }
    }

    private static Dictionary<string, string> BuildTypeLookup(IEnumerable<string> types)
    {
        return types.ToDictionary(v => v.ToLowerInvariant(), v => v, StringComparer.OrdinalIgnoreCase);
    }

    private static void ApplyTypeAliases(Dictionary<string, string> lookup, IEnumerable<string> types)
    {
        var known = new HashSet<string>(types, StringComparer.OrdinalIgnoreCase);
        foreach (var alias in TypeAliases)
        {
            if (known.Contains(alias.Value))
            {
                lookup[alias.Key] = alias.Value;
            }
        }
    }

    private static string? NormalizeTypeFromConfig(string? value)
    {
        return NormalizeTypeWithLookup(value, GetConfig().TypeLookup);
    }

    private static string? NormalizeTypeWithLookup(string? value, Dictionary<string, string> lookup)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var key = value.Trim();
        if (TypeAliases.TryGetValue(key, out var canonical))
        {
            key = canonical;
        }

        return lookup.GetValueOrDefault(key.ToLowerInvariant());
    }
}
