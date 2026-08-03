using System.Collections.Generic;

public static class TestStateFactory
{
    public static GameState CreateState(int seed = 4242)
    {
        var rng = new GameRng(seed);
        var player = new CharacterModel
        {
            Name = "Tester",
            MaxHp = 120,
            Hp = 120,
            Forza = 6,
            Difesa = 3,
            Agilita = 4,
            Fortuna = 2,
            Intelligence = 5,
            Fede = 2,
            Exp = 0,
            Mana = 30,
            MaxMana = 30,
            Level = 1,
            Moves = new List<MoveModel?> { Moves.BasicAttackMove(), null, null, null, null },
            Inventory = new Dictionary<string, int>
            {
                ["CurePiccole"] = 2,
                ["CureMedie"] = 1,
            },
            Equipment = new Dictionary<string, string?>(Items.DefaultEquipment),
            Types = new List<string> { "marziale" },
        };

        var enemy = new CharacterModel
        {
            Name = "Dummy",
            MaxHp = 40,
            Hp = 40,
            Forza = 3,
            Difesa = 1,
            Agilita = 2,
            Fortuna = 1,
            Intelligence = 1,
            Fede = 1,
            Exp = 10,
            Soli = 5,
            Moves = new List<MoveModel?> { Moves.BasicAttackMove() },
            Inventory = new Dictionary<string, int>(),
            Equipment = new Dictionary<string, string?>(Items.DefaultEquipment),
            Types = new List<string> { "marziale" },
        };

        return new GameState
        {
            Player = player,
            Enemies = new List<CharacterModel> { enemy },
            EnemyQueue = new List<CharacterModel>(),
            Rng = rng,
            Prog = new Progression(),
        };
    }
}
