using System;
using System.Collections.Generic;
using System.Linq;

public sealed class EnemySpec
{
    public string Name { get; init; } = "Goblin";
    public string Kind { get; init; } = "Mob";
    public int Hp { get; init; }
    public int Forza { get; init; }
    public int Magia { get; init; }
    public int Difesa { get; init; }
    public int Agilita { get; init; }
    public int Fortuna { get; init; }
    public int Intelligence { get; init; }
    public int Fede { get; init; }
    public int Exp { get; init; }
    public int Soli { get; init; }
    public string Sprite { get; init; } = "goblin";
    public List<string> Types { get; init; } = new();
}

public static class EnemyCatalog
{
    public const float MinibossMult = 1.6f;
    public const float BossMult = 2.0f;

    public static readonly List<EnemySpec> Mobs = new()
    {
        new EnemySpec { Name = "Goblin", Kind = "Mob", Hp = 18, Forza = 4, Magia = 1, Difesa = 1, Agilita = 5, Fortuna = 3, Intelligence = 3, Fede = 2, Exp = 6, Soli = 5, Sprite = "goblin", Types = new List<string> { "marziale" } },
        new EnemySpec { Name = "Orco", Kind = "Mob", Hp = 24, Forza = 6, Magia = 1, Difesa = 2, Agilita = 3, Fortuna = 2, Intelligence = 2, Fede = 1, Exp = 9, Soli = 8, Sprite = "orco", Types = new List<string> { "marziale" } },
        new EnemySpec { Name = "Scheletro", Kind = "Mob", Hp = 20, Forza = 5, Magia = 2, Difesa = 2, Agilita = 4, Fortuna = 4, Intelligence = 4, Fede = 3, Exp = 8, Soli = 7, Sprite = "scheletro", Types = new List<string> { "freddo" } },
        new EnemySpec { Name = "Slime", Kind = "Mob", Hp = 16, Forza = 3, Magia = 1, Difesa = 1, Agilita = 2, Fortuna = 5, Intelligence = 1, Fede = 1, Exp = 5, Soli = 4, Sprite = "slime", Types = new List<string> { "acqua" } },
        new EnemySpec { Name = "Bandito", Kind = "Mob", Hp = 22, Forza = 5, Magia = 1, Difesa = 1, Agilita = 6, Fortuna = 2, Intelligence = 5, Fede = 2, Exp = 7, Soli = 10, Sprite = "bandito", Types = new List<string> { "sparo" } },
        new EnemySpec { Name = "Stregone", Kind = "Mob", Hp = 18, Forza = 2, Magia = 6, Difesa = 1, Agilita = 4, Fortuna = 4, Intelligence = 8, Fede = 6, Exp = 10, Soli = 9, Sprite = "stregone", Types = new List<string> { "fuoco" } },
    };

    public static CharacterModel EnemyFromSpec(EnemySpec spec, GameRng rng, float mult = 1f, string namePrefix = "", string? kindOverride = null)
    {
        var hp = Math.Max(1, (int)MathF.Round(spec.Hp * mult) + 70);
        var intel = Math.Max(0, (int)MathF.Round(spec.Intelligence * mult));
        var fede = Math.Max(0, (int)MathF.Round(spec.Fede * mult));
        var level = Math.Max(1, (int)MathF.Round((Math.Max(0.25f, mult) - 1f) / 0.25f) + 1);

        var enemy = new CharacterModel
        {
            Name = $"{namePrefix}{spec.Name}",
            MaxHp = hp,
            Hp = hp,
            Forza = Math.Max(1, (int)MathF.Round(spec.Forza * mult)),
            Magia = Math.Max(0, (int)MathF.Round(spec.Magia * mult)),
            Difesa = Math.Max(0, (int)MathF.Round(spec.Difesa * mult)),
            Agilita = Math.Max(0, (int)MathF.Round(spec.Agilita * mult)),
            Fortuna = Math.Max(0, (int)MathF.Round(spec.Fortuna * mult)),
            Intelligenza = intel,
            Intelligence = intel,
            Fede = fede,
            Exp = Math.Max(0, (int)MathF.Round(spec.Exp * mult)),
            Soli = Math.Max(0, (int)MathF.Round(spec.Soli * mult)),
            Level = level,
            Sprite = spec.Sprite,
            Kind = kindOverride ?? spec.Kind,
            Types = spec.Types.ToList(),
            Moves = new List<MoveModel?>(),
            Inventory = new Dictionary<string, int>(),
            Equipment = new Dictionary<string, string?>(Items.DefaultEquipment),
        };

        AssignRandomEnemyMoves(enemy, rng);
        return enemy;
    }

    public static void AssignRandomEnemyMoves(CharacterModel enemy, GameRng rng, int movesPerEnemy = 4)
    {
        var moveList = Database.Instance.GetMoves().Values.Where(m => !m.IsBasicAttack).ToList();
        if (moveList.Count == 0)
        {
            enemy.Moves = new List<MoveModel?>();
            return;
        }

        Shuffle(moveList, rng);
        var selected = new List<MoveModel?>();
        foreach (var move in moveList)
        {
            if (WouldExceedFamilyLimit(selected, move))
            {
                continue;
            }

            selected.Add(move);
            if (selected.Count >= Math.Min(movesPerEnemy, moveList.Count))
            {
                break;
            }
        }

        enemy.Moves = selected;
        if (enemy.Moves.Count > 0)
        {
            var firstType = TypeSystem.MoveType(enemy.Moves[0]);
            if (!string.IsNullOrWhiteSpace(firstType))
            {
                enemy.Types = new List<string> { firstType! };
            }
        }
    }

    public static EnemySpec? GetMobByName(string name)
    {
        return Mobs.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static bool WouldExceedFamilyLimit(List<MoveModel?> existing, MoveModel candidate, int maxFamilies = 2)
    {
        var owned = new HashSet<string>(TypeSystem.FamiliesInMoves(existing), StringComparer.OrdinalIgnoreCase);
        var fam = TypeSystem.FamilyOfType(TypeSystem.MoveType(candidate));
        if (string.IsNullOrWhiteSpace(fam) || owned.Contains(fam!))
        {
            return false;
        }

        return owned.Count >= maxFamilies;
    }

    private static void Shuffle<T>(IList<T> list, GameRng rng)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.NextInt(0, i);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
