using System;
using System.Collections.Generic;

public static partial class Inventory
{
    public static void GrantBattleLoot(GameState state, List<CharacterModel> deadEnemies)
    {
        EnsureLootFlags(state);
        foreach (var enemy in deadEnemies)
        {
            state.BattleKills += 1;
            state.BattleLootExp += Math.Max(0, enemy.Exp);
            state.BattleClaimableSoli += Items.RollMoneyDrop(Math.Max(1, enemy.Level), state.Rng);
            state.BattleLootSoli = state.BattleClaimableSoli;
            state.BattleLootItems.Add(Items.RollItemDrop(state.Rng));
            state.BattleDefeatedEnemies.Add(enemy);
        }
    }

    public static int ClaimMoney(GameState state, CharacterModel player)
    {
        EnsureLootFlags(state);
        var amount = Math.Max(0, state.BattleClaimableSoli);
        if (state.BattleClaimedSoli || amount <= 0)
        {
            return 0;
        }

        player.Soli += amount;
        state.BattleClaimedSoli = true;
        state.BattleClaimableSoli = 0;
        state.BattleLootSoli = 0;
        return amount;
    }

    public static List<string> ClaimItems(GameState state, CharacterModel player)
    {
        EnsureLootFlags(state);
        if (state.BattleClaimedItems)
        {
            return new List<string>();
        }

        var claimed = new List<string>();
        foreach (var itemId in state.BattleLootItems)
        {
            if (!string.IsNullOrWhiteSpace(itemId))
            {
                claimed.Add(itemId);
                player.AddItem(itemId, 1);
            }
        }

        state.BattleLootItems = new List<string>();
        state.BattleClaimedItems = claimed.Count > 0;
        return claimed;
    }
}
