using System;
using System.Collections.Generic;
using System.Text.Json;

public static partial class Moves
{
    public static Dictionary<int, MoveModel> LoadFromJson(string json)
    {
        var registry = new Dictionary<int, MoveModel>();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("moves", out var movesNode) || movesNode.ValueKind != JsonValueKind.Array)
        {
            return registry;
        }

        foreach (var item in movesNode.EnumerateArray())
        {
            if (!TryParseMove(item, out var move))
            {
                continue;
            }

            registry[move.MoveId] = move;
        }

        return registry;
    }

    public static MoveModel BasicAttackMove()
    {
        var basicPower = CombatBalanceConfig.BasicAttackBasePower;
        return new MoveModel
        {
            Name = "Attacco",
            MoveId = 0,
            BaseDamage = basicPower,
            Power = basicPower,
            CostAmount = 0,
            CostResource = null,
        };
    }

    public static bool IsBasicAttack(MoveModel? move)
    {
        return move is not null && move.IsBasicAttack;
    }

    private static bool TryParseMove(JsonElement item, out MoveModel move)
    {
        move = null!;
        if (!item.TryGetProperty("id", out var idNode) || idNode.ValueKind == JsonValueKind.Null)
        {
            return false;
        }

        var moveId = TryGetInt(idNode);
        if (moveId is null)
        {
            return false;
        }

        var name = GetString(item, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var weapon = NormalizeString(GetString(item, "weapon"));
        var element = NormalizeString(GetString(item, "type"));
        var moveType = NormalizeMoveType(element ?? weapon);
        var baseDamage = TryGetInt(item, "base_damage") ?? TryGetInt(item, "power") ?? TryGetInt(item, "damage") ?? 10;
        var power = TryGetInt(item, "power") ?? baseDamage;
        var aoe = TryGetBool(item, "aoe");

        var (costResource, costRaw, costAmount, costUnit) = ParseCost(item);
        move = new MoveModel
        {
            MoveId = moveId.Value,
            Name = name.Trim(),
            Weapon = weapon,
            Element = element,
            MoveType = moveType,
            BaseDamage = baseDamage,
            Aoe = aoe,
            Power = power,
            CostResource = costResource,
            CostRaw = costRaw,
            CostAmount = costAmount,
            CostUnit = costUnit,
        };
        return true;
    }

    private static (string? Resource, string? Raw, int? Amount, string? Unit) ParseCost(JsonElement move)
    {
        if (!move.TryGetProperty("cost", out var costNode) || costNode.ValueKind != JsonValueKind.Object)
        {
            return (null, null, null, null);
        }

        var resource = NormalizeCostCategory(NormalizeString(GetString(costNode, "resource")));
        var raw = NormalizeString(GetString(costNode, "raw"));
        var unit = NormalizeString(GetString(costNode, "unit"));
        var amount = TryGetInt(costNode, "amount");
        return (resource, raw, amount, unit);
    }
}
