using System.Collections.Generic;

public static class OverworldEnemySpawnFactory
{
    private const int SpawnSeedSalt = 0x51A9;

    public static List<OverworldEnemyModel> BuildForDungeon(DungeonData dungeon, int proceduralSeed)
    {
        var rng = new GameRng(DeterministicSeed.Combine(proceduralSeed, dungeon.FloorIndex, SpawnSeedSalt));
        var list = new List<OverworldEnemyModel>();
        var floorMult = 1f + (dungeon.FloorIndex * 0.25f);
        for (var i = 0; i < dungeon.EnemySpawns.Count; i++)
        {
            var spec = EnemyCatalog.Mobs[rng.NextInt(0, EnemyCatalog.Mobs.Count - 1)];
            var spawn = dungeon.EnemySpawns[i];
            list.Add(new OverworldEnemyModel
            {
                EnemyId = $"f{dungeon.FloorIndex}_spawn_{i}",
                Name = spec.Name,
                SpecName = spec.Name,
                Sprite = spec.Sprite,
                Position = new Godot.Vector2(spawn.X, spawn.Z),
                Kind = spec.Kind,
                BattleMult = floorMult,
                Speed = 1.4f + (float)rng.NextDouble(),
                Intelligence = rng.NextInt(1, 8),
            });
        }

        if (!dungeon.HasFloorBossSpawn)
        {
            return list;
        }

        var bossSpec = EnemyCatalog.Mobs[rng.NextInt(0, EnemyCatalog.Mobs.Count - 1)];
        list.Add(new OverworldEnemyModel
        {
            EnemyId = $"f{dungeon.FloorIndex}_boss",
            Name = $"BOSS: {bossSpec.Name}",
            SpecName = bossSpec.Name,
            Sprite = bossSpec.Sprite,
            Position = new Godot.Vector2(dungeon.FloorBossSpawn.X, dungeon.FloorBossSpawn.Z),
            Kind = "Boss",
            BehaviorKind = OverworldBehavior.Ansioso,
            Aggressive = true,
            IsFloorBoss = true,
            BattleMult = floorMult + 0.35f,
            Speed = 1.8f,
            Intelligence = 7,
        });
        return list;
    }
}
