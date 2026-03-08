using System.Collections.Generic;

public sealed class CharacterModel
{
    public string Name { get; set; } = "Player";
    public int MaxHp { get; set; }
    public int Hp { get; set; }
    public int Forza { get; set; }
    public int Magia { get; set; }
    public int Difesa { get; set; }
    public int Agilita { get; set; }
    public int Fortuna { get; set; }
    public int Intelligenza { get; set; }
    public int Fede { get; set; }
    public int Intelligence { get; set; }
    public int Exp { get; set; }
    public int Soli { get; set; }
    public int Mana { get; set; }
    public int MaxMana { get; set; }
    public int Medicine { get; set; }
    public int Level { get; set; } = 1;
    public int StatPoints { get; set; }
    public string Sprite { get; set; } = "player";
    public string Kind { get; set; } = "Player";
    public List<string> Types { get; set; } = new();
    public List<MoveModel?> Moves { get; set; } = new();
    public Dictionary<string, int> Inventory { get; set; } = new();
    public Dictionary<string, string?> Equipment { get; set; } = new();
    public bool Defending { get; set; }

    public bool IsAlive => Hp > 0;

    public int TakeDamage(int raw)
    {
        var dmg = raw < 0 ? 0 : raw;
        Hp = Hp - dmg;
        if (Hp < 0)
        {
            Hp = 0;
        }

        return dmg;
    }

    public int Heal(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        var old = Hp;
        Hp += amount;
        if (Hp > MaxHp)
        {
            Hp = MaxHp;
        }

        return Hp - old;
    }

    public void AddItem(string itemId, int qty)
    {
        if (string.IsNullOrWhiteSpace(itemId) || qty <= 0)
        {
            return;
        }

        Inventory[itemId] = Inventory.GetValueOrDefault(itemId, 0) + qty;
    }

    public bool RemoveItem(string itemId, int qty)
    {
        if (string.IsNullOrWhiteSpace(itemId) || qty <= 0)
        {
            return false;
        }

        var have = Inventory.GetValueOrDefault(itemId, 0);
        if (have < qty)
        {
            return false;
        }

        var left = have - qty;
        if (left <= 0)
        {
            Inventory.Remove(itemId);
        }
        else
        {
            Inventory[itemId] = left;
        }

        return true;
    }

    public bool IsEquipped(string itemId)
    {
        foreach (var pair in Equipment)
        {
            if (pair.Value == itemId)
            {
                return true;
            }
        }

        return false;
    }
}
