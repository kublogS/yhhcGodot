using System;
using System.Collections.Generic;

public sealed class GameState
{
    public CharacterModel Player { get; set; } = new();
    public CharacterModel Enemy { get; set; } = new();
    public Progression Prog { get; set; } = new();
    public GameRng Rng { get; set; } = new(12345);
    public List<CharacterModel> Enemies { get; set; } = new();
    public List<CharacterModel> EnemyQueue { get; set; } = new();
    public int BattleKills { get; set; }
    public int BattleLootExp { get; set; }
    public int BattleLootSoli { get; set; }
    public int BattleClaimableSoli { get; set; }
    public List<string> BattleLootItems { get; set; } = new();
    public bool BattleClaimedSoli { get; set; }
    public bool BattleClaimedItems { get; set; }
    public List<CharacterModel> BattleDefeatedEnemies { get; set; } = new();
    public int BattleStealTokens { get; set; }

    public bool BattleHasCapacity() => Enemies.Count < 4;

    public void SyncEnemyLegacy()
    {
        if (Enemies.Count > 0)
        {
            Enemy = Enemies[0];
        }
    }

    public void AddEnemyToBattle(CharacterModel enemy)
    {
        if (BattleHasCapacity())
        {
            Enemies.Add(enemy);
        }
        else
        {
            EnemyQueue.Add(enemy);
        }

        SyncEnemyLegacy();
    }

    public int PopNextFromQueueIfPossible()
    {
        var moved = 0;
        while (BattleHasCapacity() && EnemyQueue.Count > 0)
        {
            Enemies.Add(EnemyQueue[0]);
            EnemyQueue.RemoveAt(0);
            moved++;
        }

        SyncEnemyLegacy();
        return moved;
    }

    public void SetBattleGroups(List<CharacterModel> enemies, List<CharacterModel>? queued = null)
    {
        Enemies = new List<CharacterModel>(enemies.GetRange(0, Math.Min(4, enemies.Count)));
        EnemyQueue = enemies.Count > 4 ? new List<CharacterModel>(enemies.GetRange(4, enemies.Count - 4)) : new List<CharacterModel>();
        if (queued is not null)
        {
            EnemyQueue.AddRange(queued);
        }

        SyncEnemyLegacy();
    }

    public void ResetBattleInstance()
    {
        BattleKills = 0;
        BattleLootExp = 0;
        BattleLootSoli = 0;
        BattleClaimableSoli = 0;
        BattleLootItems = new List<string>();
        BattleClaimedSoli = false;
        BattleClaimedItems = false;
        BattleDefeatedEnemies = new List<CharacterModel>();
        BattleStealTokens = 0;
    }

    public CharacterModel RespawnEnemy()
    {
        Enemy = SpawnNextEnemy(Rng, Prog);
        Enemies = new List<CharacterModel> { Enemy };
        EnemyQueue = new List<CharacterModel>();
        return Enemy;
    }

    public static GameState New(string playerName, int? seed = null)
    {
        var rng = new GameRng(seed ?? (int)TimeSeed());
        var player = CreateDefaultPlayer(playerName);
        var prog = new Progression
        {
            MobsSinceMiniboss = 0,
            MinibossSinceBoss = 0,
            NextMinibossIn = rng.NextInt(5, 8),
        };

        var enemy = SpawnNextEnemy(rng, prog);
        return new GameState
        {
            Player = player,
            Enemy = enemy,
            Enemies = new List<CharacterModel> { enemy },
            EnemyQueue = new List<CharacterModel>(),
            Rng = rng,
            Prog = prog,
        };
    }

    public static CharacterModel SpawnNextEnemy(GameRng rng, Progression prog)
    {
        if (prog.MinibossSinceBoss >= 3)
        {
            var boss = EnemyCatalog.EnemyFromSpec(rng.Choose(EnemyCatalog.Mobs), rng, EnemyCatalog.BossMult, "BOSS: ", "Boss");
            prog.MinibossSinceBoss = 0;
            prog.MobsSinceMiniboss = 0;
            prog.NextMinibossIn = rng.NextInt(5, 8);
            return boss;
        }

        if (prog.MobsSinceMiniboss >= prog.NextMinibossIn)
        {
            var miniboss = EnemyCatalog.EnemyFromSpec(rng.Choose(EnemyCatalog.Mobs), rng, EnemyCatalog.MinibossMult, "MiniBoss: ", "MiniBoss");
            prog.MobsSinceMiniboss = 0;
            prog.MinibossSinceBoss += 1;
            prog.NextMinibossIn = rng.NextInt(5, 8);
            return miniboss;
        }

        prog.MobsSinceMiniboss += 1;
        return EnemyCatalog.EnemyFromSpec(rng.Choose(EnemyCatalog.Mobs), rng, 1f, string.Empty, "Mob");
    }

    private static CharacterModel CreateDefaultPlayer(string name)
    {
        var player = new CharacterModel
        {
            Name = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim(),
            MaxHp = 130,
            Hp = 130,
            Forza = 5,
            Magia = 2,
            Difesa = 2,
            Agilita = 4,
            Fortuna = 4,
            Intelligenza = 10,
            Intelligence = 10,
            Fede = 10,
            Exp = 0,
            Soli = 0,
            Mana = 60,
            MaxMana = 60,
            Level = 1,
            StatPoints = 0,
            Sprite = "player",
            Kind = "Player",
            Types = new List<string> { "marziale" },
            Inventory = new Dictionary<string, int> { ["CureMedie"] = 5 },
            Equipment = new Dictionary<string, string?>(Items.DefaultEquipment),
            Moves = new List<MoveModel?> { Moves.BasicAttackMove(), null, null, null, null },
        };
        return player;
    }

    private static long TimeSeed()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
