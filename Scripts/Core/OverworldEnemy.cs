using System.Collections.Generic;

public sealed class OverworldEnemyModel
{
    public string EnemyId { get; set; } = "enemy";
    public string Name { get; set; } = "Enemy";
    public string Sprite { get; set; } = "enemy";
    public float X { get; set; }
    public float Y { get; set; }
    public bool Aggressive { get; set; } = true;
    public bool Active { get; set; } = true;
    public string Kind { get; set; } = "Mob";
    public string SpecName { get; set; } = "Goblin";
    public string Behavior { get; set; } = "Sospetto";
    public float SpeedX { get; set; } = 1.6f;
    public int IntelligenceY { get; set; } = 1;
    public bool Alerted { get; set; }
    public bool IsStuck { get; set; }
    public float StuckTimer { get; set; }
    public float WakeTimer { get; set; }
    public string? SpeechText { get; set; }
    public float SpeechTimer { get; set; }
    public float BattleMult { get; set; } = 1f;
    public bool IsFloorBoss { get; set; }

    public CharacterModel ToBattleCharacter(GameRng rng)
    {
        var spec = EnemyCatalog.GetMobByName(SpecName) ?? EnemyCatalog.Mobs[0];
        var enemy = EnemyCatalog.EnemyFromSpec(spec, rng, BattleMult, string.Empty, Kind);
        enemy.Name = string.IsNullOrWhiteSpace(Name) ? spec.Name : Name;
        enemy.Sprite = string.IsNullOrWhiteSpace(Sprite) ? enemy.Sprite : Sprite;
        if (enemy.Moves.Count == 0)
        {
            enemy.Moves = new List<MoveModel?> { Moves.BasicAttackMove() };
        }

        return enemy;
    }
}
