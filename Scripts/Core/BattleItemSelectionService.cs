using System;
using System.Linq;

public static class BattleItemSelectionService
{
    public static string? ResolveConsumable(CharacterModel player, string? requestedItemId)
    {
        if (!string.IsNullOrWhiteSpace(requestedItemId) && player.Inventory.GetValueOrDefault(requestedItemId, 0) > 0)
        {
            return requestedItemId;
        }

        return player.Inventory
            .Where(entry => entry.Value > 0 && Items.GetItemDef(entry.Key)?.Kind == "consumable")
            .OrderByDescending(entry => Items.GetItemDef(entry.Key)?.HealAmount ?? 0)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => entry.Key)
            .FirstOrDefault();
    }
}
