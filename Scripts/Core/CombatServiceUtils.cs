using System;
using System.Collections.Generic;

public static partial class CombatService
{
    internal static bool CanCastMove(CharacterModel owner, MoveModel move)
    {
        var amount = move.CostAmount ?? 0;
        var category = (move.CostResource ?? string.Empty).ToLowerInvariant();
        return amount <= 0 || category switch
        {
            "mana" => owner.Mana >= amount,
            "vita" => owner.Hp > amount,
            "exp" => owner.Exp >= amount,
            _ => true,
        };
    }

    internal static void ApplyMoveCost(CharacterModel owner, MoveModel move)
    {
        var amount = move.CostAmount ?? 0;
        var category = (move.CostResource ?? string.Empty).ToLowerInvariant();
        if (amount <= 0)
        {
            return;
        }

        if (category == "mana") owner.Mana = Math.Max(0, owner.Mana - amount);
        if (category == "vita") owner.Hp = Math.Max(0, owner.Hp - amount);
        if (category == "exp") owner.Exp = Math.Max(0, owner.Exp - amount);
    }

    internal static bool TryFlee(GameState state)
    {
        if (state.Rng.NextDouble() > CombatBalanceConfig.BaseFleeSuccessChance)
        {
            return false;
        }

        state.Enemies.Clear();
        state.EnemyQueue.Clear();
        state.ResetBattleInstance();
        return true;
    }

    internal static string FormatTags(DamageTags tags)
    {
        var labels = new List<string>();
        if (tags.Weak) labels.Add("debolezza");
        if (tags.Nullified) labels.Add("annullato");
        if (tags.Crit) labels.Add("critico");
        return labels.Count > 0 ? $" [{string.Join(", ", labels)}]" : string.Empty;
    }

    internal static Dictionary<string, string> BuildTagMetadata(DamageTags tags)
    {
        return new Dictionary<string, string>
        {
            ["attackType"] = tags.AttackType ?? string.Empty,
            ["weak"] = tags.Weak.ToString(),
            ["nullified"] = tags.Nullified.ToString(),
            ["sameFamily"] = tags.SameFamily.ToString(),
            ["enemyFamily"] = tags.EnemyFamily.ToString(),
            ["stab"] = tags.Stab.ToString(),
            ["crit"] = tags.Crit.ToString(),
        };
    }
}
