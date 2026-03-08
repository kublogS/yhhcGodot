using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public static partial class TypeSystem
{
    private static void LoadFamilies(JsonElement root, TypeSystemConfig cfg)
    {
        if (!root.TryGetProperty("families", out var famNode) || famNode.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var family in famNode.EnumerateObject())
        {
            var members = new List<string>();
            foreach (var raw in family.Value.EnumerateArray())
            {
                var normalized = NormalizeTypeWithLookup(raw.GetString(), cfg.TypeLookup);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    members.Add(normalized!);
                    cfg.TypeToFamily.TryAdd(normalized!, family.Name);
                }
            }

            cfg.Families[family.Name] = members;
        }
    }

    private static void LoadWeakness(JsonElement root, TypeSystemConfig cfg)
    {
        if (!root.TryGetProperty("weakness", out var weakNode) || weakNode.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var weak in weakNode.EnumerateObject())
        {
            var defender = NormalizeTypeWithLookup(weak.Name, cfg.TypeLookup);
            if (string.IsNullOrWhiteSpace(defender))
            {
                continue;
            }

            var attacks = new List<string>();
            foreach (var raw in weak.Value.EnumerateArray())
            {
                var attack = NormalizeTypeWithLookup(raw.GetString(), cfg.TypeLookup);
                if (!string.IsNullOrWhiteSpace(attack))
                {
                    attacks.Add(attack!);
                }
            }

            cfg.Weakness[defender!] = attacks;
        }
    }

    private static void LoadFriendGroups(JsonElement root, TypeSystemConfig cfg)
    {
        if (!root.TryGetProperty("friend_groups", out var groupsNode) || groupsNode.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var index = 0;
        foreach (var grp in groupsNode.EnumerateArray())
        {
            var group = grp.EnumerateArray()
                .Select(v => v.GetString() ?? string.Empty)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();
            cfg.FriendGroups.Add(group);
            foreach (var family in group)
            {
                cfg.FamilyGroup[family] = index;
            }

            index++;
        }
    }

    private static void LoadStatScaling(JsonElement root, TypeSystemConfig cfg)
    {
        if (!root.TryGetProperty("type_stat_scaling", out var statsNode) || statsNode.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var stat in statsNode.EnumerateObject())
        {
            var type = NormalizeTypeWithLookup(stat.Name, cfg.TypeLookup);
            if (!string.IsNullOrWhiteSpace(type))
            {
                cfg.TypeStatScaling[type!] = (stat.Value.GetString() ?? "attacco").Trim().ToLowerInvariant();
            }
        }
    }

    private static void LoadScaling(JsonElement root, TypeSystemConfig cfg)
    {
        if (!root.TryGetProperty("scaling", out var scalingNode) || scalingNode.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var value in scalingNode.EnumerateObject())
        {
            if (value.Value.TryGetSingle(out var parsed))
            {
                cfg.Scaling[value.Name] = parsed;
            }
        }
    }

    private static void LoadModifiers(JsonElement root, TypeSystemConfig cfg)
    {
        if (!root.TryGetProperty("damage_modifiers", out var modsNode) || modsNode.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var value in modsNode.EnumerateObject())
        {
            cfg.DamageModifiers[value.Name] = value.Value.ToString();
        }
    }
}
