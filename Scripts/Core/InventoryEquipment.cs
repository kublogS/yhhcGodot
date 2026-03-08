using System;

public static partial class Inventory
{
    public static bool EquipItem(CharacterModel player, string itemId)
    {
        EnsurePlayerInventory(player);
        var info = Items.GetItemDef(itemId);
        if (info is null || info.Kind != "equipment" || string.IsNullOrWhiteSpace(info.EquipSlot))
        {
            return false;
        }

        var slot = info.EquipSlot!;
        if (!Array.Exists(Items.EquipmentSlots, e => e == slot))
        {
            return false;
        }

        if (player.Inventory.GetValueOrDefault(itemId, 0) <= 0 && !player.IsEquipped(itemId))
        {
            return false;
        }

        var current = player.Equipment.GetValueOrDefault(slot);
        if (current == itemId)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(current))
        {
            ApplyItemBonus(player, current!, -1);
        }

        player.Equipment[slot] = itemId;
        ApplyItemBonus(player, itemId, 1);
        return true;
    }

    public static bool UnequipSlot(CharacterModel player, string slot)
    {
        EnsurePlayerInventory(player);
        if (!Array.Exists(Items.EquipmentSlots, e => e == slot))
        {
            return false;
        }

        var current = player.Equipment.GetValueOrDefault(slot);
        if (string.IsNullOrWhiteSpace(current))
        {
            return false;
        }

        ApplyItemBonus(player, current!, -1);
        player.Equipment[slot] = null;
        return true;
    }

    private static void ApplyItemBonus(CharacterModel player, string itemId, int sign)
    {
        var info = Items.GetItemDef(itemId);
        if (info is null)
        {
            return;
        }

        foreach (var (stat, value) in info.Bonuses)
        {
            var delta = value * sign;
            switch (stat)
            {
                case "forza":
                    player.Forza = Math.Max(0, player.Forza + delta);
                    break;
                case "magia":
                    player.Magia = Math.Max(0, player.Magia + delta);
                    break;
                case "difesa":
                    player.Difesa = Math.Max(0, player.Difesa + delta);
                    break;
                case "fede":
                    player.Fede = Math.Max(0, player.Fede + delta);
                    break;
                case "intelligenza":
                    player.Intelligenza = Math.Max(0, player.Intelligenza + delta);
                    break;
                case "intelligence":
                    player.Intelligence = Math.Max(0, player.Intelligence + delta);
                    break;
            }
        }
    }
}
