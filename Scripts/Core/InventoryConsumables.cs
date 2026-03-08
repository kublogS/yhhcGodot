public static partial class Inventory
{
    public static int UseConsumable(CharacterModel player, string itemId)
    {
        EnsurePlayerInventory(player);
        var info = Items.GetItemDef(itemId);
        if (info is null || info.Kind != "consumable" || info.HealAmount <= 0 || player.Hp >= player.MaxHp)
        {
            return 0;
        }

        if (!player.RemoveItem(itemId, 1))
        {
            return 0;
        }

        return player.Heal(info.HealAmount);
    }
}
