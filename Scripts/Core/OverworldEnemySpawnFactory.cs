using System.Collections.Generic;

public static class OverworldEnemySpawnFactory
{
    private const int SpawnSeedSalt = 0x51A9;

    // Smallest repeating block that satisfies "1 Normale every 2 Allerta"
    // and "1 Pacifico every 3 Normale" simultaneously: 3 Normale needs 6
    // Allerta (3*2) and 1 Pacifico (3/3). Cycled by spawn index, so it only
    // approximates the ratio on floors with few enemies and converges on it
    // as the floor gets busier.
    private static readonly OverworldBehavior[] BehaviorCycle =
    {
        OverworldBehavior.Allerta,
        OverworldBehavior.Allerta,
        OverworldBehavior.Normale,
        OverworldBehavior.Allerta,
        OverworldBehavior.Allerta,
        OverworldBehavior.Normale,
        OverworldBehavior.Allerta,
        OverworldBehavior.Allerta,
        OverworldBehavior.Normale,
        OverworldBehavior.Pacifico,
    };

    public static List<OverworldEnemyModel> BuildForDungeon(DungeonData dungeon, int proceduralSeed)
    {
        var rng = new GameRng(DeterministicSeed.Combine(proceduralSeed, dungeon.FloorIndex, SpawnSeedSalt));
        var list = new List<OverworldEnemyModel>();
        var floorMult = 1f + (dungeon.FloorIndex * 0.25f);

        // One Paranoico per floor: reserve the first spawn point for it
        // instead of adding a dungeon-generator dependency for a dedicated
        // spawn slot.
        var paranoidIndex = dungeon.EnemySpawns.Count > 0 ? 0 : -1;
        var behaviorCursor = 0;

        for (var i = 0; i < dungeon.EnemySpawns.Count; i++)
        {
            var spec = EnemyCatalog.Mobs[rng.NextInt(0, EnemyCatalog.Mobs.Count - 1)];
            var spawn = dungeon.EnemySpawns[i];
            var behavior = i == paranoidIndex ? OverworldBehavior.Paranoico : BehaviorCycle[behaviorCursor % BehaviorCycle.Length];
            if (i != paranoidIndex)
            {
                behaviorCursor++;
            }

            list.Add(new OverworldEnemyModel
            {
                EnemyId = $"f{dungeon.FloorIndex}_spawn_{i}",
                Name = spec.Name,
                SpecName = spec.Name,
                Sprite = spec.Sprite,
                Position = new Godot.Vector2(spawn.X, spawn.Z),
                Kind = spec.Kind,
                BehaviorKind = behavior,
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
            BehaviorKind = OverworldBehavior.Allerta,
            Aggressive = true,
            IsFloorBoss = true,
            BattleMult = floorMult * EnemyCatalog.BossMult,
            Speed = 1.8f,
            Intelligence = 7,
        });
        return list;
    }
}
