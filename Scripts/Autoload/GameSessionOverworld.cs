using System;
using System.Collections.Generic;
using Godot;

public partial class GameSession
{
    public void BeginOverworldRun()
    {
        if (State is null)
        {
            return;
        }

        RunActive = true;
        ProcFloor = 0;
        ProcSeed = State.Rng.NextInt(1, 9_999_999);
        GenerateCurrentFloor();
    }

    public void GenerateNewDungeon()
    {
        if (State is null)
        {
            return;
        }

        RunActive = true;
        if (ProcSeed <= 0)
        {
            ProcSeed = State.Rng.NextInt(1, 9_999_999);
        }

        ProcFloor = Math.Max(0, ProcFloor);
        GenerateCurrentFloor();
    }

    public bool TryAdvanceOverworldFloor()
    {
        if (State is null || !RunActive)
        {
            return false;
        }

        ProcFloor++;
        if (ProcFloor >= ProcMaxFloors)
        {
            LabyrinthCompletions++;
            ReturnToSpawnHub();
            return false;
        }

        GenerateCurrentFloor();
        return true;
    }

    public void ReturnToSpawnHub()
    {
        RunActive = false;
        HasEnteredOverworld = false;
        ProcSeed = 0;
        ProcFloor = 0;
        CurrentDungeon = null;
        OverworldEnemies.Clear();
        PlayerWorldPosition = Vector3.Zero;
        PlayerYawRadians = 0f;
    }

    private void GenerateCurrentFloor()
    {
        if (State is null)
        {
            return;
        }

        CurrentDungeon = DungeonGenerator.Generate(ProcSeed, ProcFloor);
        PlayerWorldPosition = CurrentDungeon.PlayerSpawn + new Vector3(0f, 1.2f, 0f);
        PlayerYawRadians = 0f;
        OverworldEnemies = BuildOverworldEnemiesFromDungeon(CurrentDungeon, State.Rng);
        HasEnteredOverworld = true;
        DeepestFloor = Math.Max(DeepestFloor, ProcFloor + 1);
    }

    private static List<OverworldEnemyModel> BuildOverworldEnemiesFromDungeon(DungeonData dungeon, GameRng rng)
    {
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
                X = spawn.X,
                Y = spawn.Z,
                Kind = spec.Kind,
                BattleMult = floorMult,
                SpeedX = 1.4f + (float)rng.NextDouble(),
                IntelligenceY = rng.NextInt(1, 8),
            });
        }

        if (dungeon.HasFloorBossSpawn)
        {
            var spec = EnemyCatalog.Mobs[rng.NextInt(0, EnemyCatalog.Mobs.Count - 1)];
            list.Add(new OverworldEnemyModel
            {
                EnemyId = $"f{dungeon.FloorIndex}_boss",
                Name = $"BOSS: {spec.Name}",
                SpecName = spec.Name,
                Sprite = spec.Sprite,
                X = dungeon.FloorBossSpawn.X,
                Y = dungeon.FloorBossSpawn.Z,
                Kind = "Boss",
                Behavior = "Ansioso",
                Aggressive = true,
                IsFloorBoss = true,
                BattleMult = floorMult + 0.35f,
                SpeedX = 1.8f,
                IntelligenceY = 7,
            });
        }

        return list;
    }
}
