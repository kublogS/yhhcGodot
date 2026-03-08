using System;
using System.Collections.Generic;

public sealed class ItemDef
{
    public string ItemId { get; init; } = "";
    public string Kind { get; init; } = "consumable";
    public int HealAmount { get; init; }
    public string? EquipSlot { get; init; }
    public Dictionary<string, int> Bonuses { get; init; } = new();
}

public static class Items
{
    public static readonly string[] EquipmentSlots =
    {
        "arma", "armatura", "ciondolo", "libro", "testo_sacro",
    };

    public static readonly Dictionary<string, string?> DefaultEquipment = new(StringComparer.Ordinal)
    {
        ["arma"] = null,
        ["armatura"] = null,
        ["ciondolo"] = null,
        ["libro"] = null,
        ["testo_sacro"] = null,
    };

    public static readonly Dictionary<string, ItemDef> ItemDefs = new(StringComparer.Ordinal)
    {
        ["CurePiccole"] = new ItemDef { ItemId = "CurePiccole", Kind = "consumable", HealAmount = 20 },
        ["CureMedie"] = new ItemDef { ItemId = "CureMedie", Kind = "consumable", HealAmount = 40 },
        ["CureGrandi"] = new ItemDef { ItemId = "CureGrandi", Kind = "consumable", HealAmount = 100 },
        ["ArmaturaX"] = new ItemDef
        {
            ItemId = "ArmaturaX",
            Kind = "equipment",
            EquipSlot = "armatura",
            Bonuses = new Dictionary<string, int> { ["difesa"] = 5 },
        },
        ["ArmaX"] = new ItemDef
        {
            ItemId = "ArmaX",
            Kind = "equipment",
            EquipSlot = "arma",
            Bonuses = new Dictionary<string, int> { ["forza"] = 5 },
        },
        ["CiondoloX"] = new ItemDef
        {
            ItemId = "CiondoloX",
            Kind = "equipment",
            EquipSlot = "ciondolo",
            Bonuses = new Dictionary<string, int> { ["magia"] = 5 },
        },
        ["LibroX"] = new ItemDef
        {
            ItemId = "LibroX",
            Kind = "equipment",
            EquipSlot = "libro",
            Bonuses = new Dictionary<string, int> { ["intelligenza"] = 10, ["intelligence"] = 10 },
        },
        ["TestoSacroX"] = new ItemDef
        {
            ItemId = "TestoSacroX",
            Kind = "equipment",
            EquipSlot = "testo_sacro",
            Bonuses = new Dictionary<string, int> { ["fede"] = 10 },
        },
    };

    private static readonly string[] RareDropGroup =
    {
        "CureGrandi", "ArmaturaX", "ArmaX", "CiondoloX", "LibroX", "TestoSacroX",
    };

    public static ItemDef? GetItemDef(string itemId)
    {
        return ItemDefs.GetValueOrDefault(itemId);
    }

    public static int RollMoneyDrop(int enemyLevel, GameRng rng)
    {
        var level = Math.Max(1, enemyLevel);
        var coeff = rng.NextInt(1, 4);
        return Math.Max(0, level * coeff);
    }

    public static string RollItemDrop(GameRng rng)
    {
        var p = rng.NextDouble();
        if (p < 0.45)
        {
            return "CurePiccole";
        }

        if (p < 0.75)
        {
            return "CureMedie";
        }

        return RareDropGroup[rng.NextInt(0, RareDropGroup.Length - 1)];
    }

    public static string ItemDisplayName(string itemId)
    {
        return GetItemDef(itemId)?.ItemId ?? itemId;
    }
}
