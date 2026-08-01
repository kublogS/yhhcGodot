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
}
