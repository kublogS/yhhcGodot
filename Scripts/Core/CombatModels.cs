using System.Collections.Generic;

public enum CombatActionType
{
    Attack,
    Defend,
    Items,
    Flee,
}

public sealed class AttackResult
{
    public int RawDamage { get; set; }
    public string Kind { get; set; } = "normale";
}

public sealed class DamageTags
{
    public string? AttackType { get; set; }
    public bool Weak { get; set; }
    public bool Nullified { get; set; }
    public bool SameFamily { get; set; }
    public bool EnemyFamily { get; set; }
    public bool Stab { get; set; }
    public bool Crit { get; set; }
}

public sealed class CombatTurnRequest
{
    public CombatActionType ActionType { get; set; } = CombatActionType.Attack;
    public int SelectedMoveIndex { get; set; }
    public int SelectedTargetIndex { get; set; }
    public string? SelectedItemId { get; set; }
}

public sealed class CombatTurnOutcome
{
    public List<string> LogLines { get; set; } = new();
    public List<CombatEventEntry> Events { get; set; } = new();
    public bool PlayerDefeated { get; set; }
    public bool BattleEnded { get; set; }
    public bool Fled { get; set; }
    public List<CharacterModel> DeadEnemies { get; set; } = new();
    public List<CharacterModel> EnteredEnemies { get; set; } = new();
}
