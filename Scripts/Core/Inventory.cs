using System;
using System.Collections.Generic;

public static partial class Inventory
{
    public static string LoadItemDescription(string itemId)
    {
        return Database.Instance.GetItemDescription(itemId);
    }

    private static void EnsurePlayerInventory(CharacterModel player)
    {
        if (player.Equipment.Count == 0)
        {
            player.Equipment = new Dictionary<string, string?>(Items.DefaultEquipment);
        }

        foreach (var slot in Items.EquipmentSlots)
        {
            player.Equipment.TryAdd(slot, null);
        }
    }

    private static void EnsureLootFlags(GameState state)
    {
        state.BattleLootItems ??= new List<string>();
        state.BattleClaimableSoli = Math.Max(0, state.BattleClaimableSoli);
        state.BattleLootSoli = Math.Max(0, state.BattleLootSoli);
    }
}
