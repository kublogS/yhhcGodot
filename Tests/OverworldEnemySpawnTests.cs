using System.Linq;
using Godot;

public static class OverworldEnemySpawnTests
{
    public static void DeterministicBySeedAndFloor(TestAssert assert)
    {
        const int seed = 321654;
        const int floor = 3;
        var dungeon = DungeonGenerator.Generate(seed, floor);
        var first = OverworldEnemySpawnFactory.BuildForDungeon(dungeon, seed);
        var second = OverworldEnemySpawnFactory.BuildForDungeon(dungeon, seed);
        assert.Equal(first.Count, second.Count, "spawned enemy count must be deterministic");
        for (var i = 0; i < first.Count; i++)
        {
            assert.Equal(first[i].EnemyId, second[i].EnemyId, $"enemy id must match at {i}");
            assert.Equal(first[i].Name, second[i].Name, $"enemy name must match at {i}");
            assert.Equal(first[i].Sprite, second[i].Sprite, $"enemy sprite must match at {i}");
            assert.Equal(first[i].Intelligence, second[i].Intelligence, $"enemy intelligence must match at {i}");
            assert.Equal(first[i].Speed, second[i].Speed, $"enemy speed must match at {i}");
            assert.Equal(first[i].Position, second[i].Position, $"enemy position must match at {i}");
        }

        var changed = OverworldEnemySpawnFactory.BuildForDungeon(dungeon, seed + 1);
        var anyDifference = first.Count != changed.Count || first.Zip(changed, (left, right) =>
            left.Name != right.Name
            || left.Sprite != right.Sprite
            || left.Intelligence != right.Intelligence
            || left.Speed != right.Speed).Any(diff => diff);
        assert.True(anyDifference, "changing the procedural seed should affect overworld enemy roster");
    }

    public static void OneParanoicoPerFloorAndAllertaMajority(TestAssert assert)
    {
        const int seed = 778899;
        const int floor = 5;
        var dungeon = DungeonGenerator.Generate(seed, floor);
        var spawns = OverworldEnemySpawnFactory.BuildForDungeon(dungeon, seed)
            .Where(e => !e.IsFloorBoss)
            .ToList();

        var paranoicoCount = spawns.Count(e => e.BehaviorKind == OverworldBehavior.Paranoico);
        var normaleCount = spawns.Count(e => e.BehaviorKind == OverworldBehavior.Normale);
        var allertaCount = spawns.Count(e => e.BehaviorKind == OverworldBehavior.Allerta);
        var pacificoCount = spawns.Count(e => e.BehaviorKind == OverworldBehavior.Pacifico);

        assert.Equal(1, paranoicoCount, "exactly one paranoico should spawn per floor");
        assert.True(normaleCount + allertaCount + pacificoCount == spawns.Count - 1, "every non-paranoico spawn must fall into normale/allerta/pacifico");
        if (spawns.Count >= 11)
        {
            assert.True(allertaCount >= normaleCount && allertaCount >= pacificoCount, "allerta should be the plurality behavior (1 normale every 2 allerta) once there are enough spawns for a full ratio cycle");
            assert.True(normaleCount > 0, "normale should appear once there are enough spawns for a full ratio cycle");
            assert.True(pacificoCount > 0, "pacifico should appear once there are enough spawns for a full ratio cycle");
        }
    }
}
