using System;
using System.Collections.Generic;
using System.Linq;

public static partial class CombatService
{
    private static MoveModel? PickCastableEnemyMove(CharacterModel enemy, GameState state)
    {
        var usable = enemy.Moves.Where(m => m is not null && CanCastMove(enemy, m!)).ToList();
        return usable.Count == 0 ? null : usable[state.Rng.NextInt(0, usable.Count - 1)];
    }

    private static bool CanCastMove(CharacterModel owner, MoveModel move)
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

    private static void ApplyMoveCost(CharacterModel owner, MoveModel move)
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

    private static bool TryFlee(GameState state)
    {
        if (state.Rng.NextDouble() > CombatBalanceConfig.BaseFleeSuccessChance)
        {
            return false;
        }

        state.Enemies.Clear();
        state.EnemyQueue.Clear();
        state.SyncEnemyLegacy();
        state.ResetBattleInstance();
        return true;
    }

    private static string FormatTags(DamageTags tags)
    {
        var labels = new List<string>();
        if (tags.Weak) labels.Add("debolezza");
        if (tags.Nullified) labels.Add("annullato");
        if (tags.Crit) labels.Add("critico");
        return labels.Count > 0 ? $" [{string.Join(", ", labels)}]" : string.Empty;
    }
}
