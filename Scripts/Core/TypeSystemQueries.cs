using System;
using System.Collections.Generic;
using System.Linq;

public static partial class TypeSystem
{
    public static string? MoveType(MoveModel? move)
    {
        if (move is null)
        {
            return null;
        }

        return NormalizeTypeFromConfig(move.MoveType)
               ?? NormalizeTypeFromConfig(move.Element)
               ?? NormalizeTypeFromConfig(move.Weapon);
    }

    public static string? FamilyOfType(string? typeName)
    {
        var normalized = NormalizeTypeFromConfig(typeName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return GetConfig().TypeToFamily.GetValueOrDefault(normalized!);
    }

    public static bool IsWeak(string defenderType, string attackType)
    {
        var d = NormalizeTypeFromConfig(defenderType);
        var a = NormalizeTypeFromConfig(attackType);
        return !string.IsNullOrWhiteSpace(d)
               && !string.IsNullOrWhiteSpace(a)
               && GetConfig().Weakness.GetValueOrDefault(d!)?.Contains(a!) == true;
    }

    public static bool FamiliesAreEnemy(string f1, string f2)
    {
        if (string.IsNullOrWhiteSpace(f1)
            || string.IsNullOrWhiteSpace(f2)
            || f1.Equals(f2, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var cfg = GetConfig();
        var g1 = cfg.FamilyGroup.GetValueOrDefault(f1, -1);
        var g2 = cfg.FamilyGroup.GetValueOrDefault(f2, -1);
        return g1 >= 0 && g2 >= 0 && g1 != g2;
    }

    public static string? PrimaryType(CharacterModel character)
    {
        foreach (var type in character.Types)
        {
            var normalized = NormalizeTypeFromConfig(type);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }
        }

        foreach (var move in character.Moves)
        {
            var mt = MoveType(move);
            if (!string.IsNullOrWhiteSpace(mt))
            {
                return mt;
            }
        }

        return null;
    }

    public static List<string> FamiliesInMoves(IEnumerable<MoveModel?> moves)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var move in moves)
        {
            var fam = FamilyOfType(MoveType(move));
            if (!string.IsNullOrWhiteSpace(fam))
            {
                set.Add(fam!);
            }
        }

        return set.ToList();
    }
}
