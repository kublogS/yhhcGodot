using System.Collections.Generic;
using Godot;

public static class OverworldAiParityTests
{
    private static DungeonData MakeOpenDungeon(int width, int height)
    {
        var grid = new int[height, width];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                grid[y, x] = (int)TileType.Floor;
            }
        }

        return new DungeonData { Grid = grid };
    }

    public static void PacificoNeverEngages(TestAssert assert)
    {
        var dungeon = MakeOpenDungeon(5, 5);
        var enemy = new OverworldEnemyModel
        {
            EnemyId = "pac_1",
            BehaviorKind = OverworldBehavior.Pacifico,
            Intelligence = 4,
            Speed = 1.5f,
            X = 1.5f,
            Y = 1.5f,
        };

        var decision = OverworldAI.ComputeVelocity(enemy, playerX: 2.5f, playerY: 1.5f, dungeon, alertTimer: 5f);
        assert.True(!decision.Chasing, "pacifico must never chase, even with clear line of sight");
        assert.True(!OverworldAI.ShouldStartEncounterOnContact(enemy, chasing: true, alertTimer: 5f), "pacifico must never start an encounter on contact");
    }

    public static void AllertaChasesOnlyWhileVisible(TestAssert assert)
    {
        var dungeon = MakeOpenDungeon(6, 3);
        var enemy = new OverworldEnemyModel
        {
            EnemyId = "allerta_1",
            BehaviorKind = OverworldBehavior.Allerta,
            Intelligence = 4,
            Speed = 1.5f,
            X = 1f,
            Y = 1f,
        };

        var visible = OverworldAI.ComputeVelocity(enemy, playerX: 9f, playerY: 1f, dungeon, alertTimer: 0f);
        assert.True(visible.Chasing, "allerta should chase the instant it sees the player");

        var wallGrid = new int[3, 6];
        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < 6; x++)
            {
                wallGrid[y, x] = x == 2 ? (int)TileType.Wall : (int)TileType.Floor;
            }
        }

        var blockedDungeon = new DungeonData { Grid = wallGrid };
        var blocked = OverworldAI.ComputeVelocity(enemy, playerX: 9f, playerY: 1f, blockedDungeon, alertTimer: 0f);
        assert.True(!blocked.Chasing, "allerta should stop chasing once line of sight is actually blocked");
    }

    public static void NormaleOnlyEngagesAsReinforcement(TestAssert assert)
    {
        var dungeon = MakeOpenDungeon(5, 5);
        var enemy = new OverworldEnemyModel
        {
            EnemyId = "norm_1",
            BehaviorKind = OverworldBehavior.Normale,
            Intelligence = 4,
            Speed = 1.5f,
            X = 1.5f,
            Y = 1.5f,
        };

        var decision = OverworldAI.ComputeVelocity(enemy, playerX: 2.5f, playerY: 1.5f, dungeon, alertTimer: 0f);
        assert.True(!decision.Chasing, "normale must not chase on its own line of sight");
        assert.True(OverworldAI.ShouldStartEncounterOnContact(enemy, chasing: false, alertTimer: 0f), "normale should still fight if the player walks right into it");

        var alerta = new OverworldEnemyModel { EnemyId = "ally_allerta", BehaviorKind = OverworldBehavior.Allerta, Active = true, X = 1.6f, Y = 1.5f };
        var pacifico = new OverworldEnemyModel { EnemyId = "ally_pacifico", BehaviorKind = OverworldBehavior.Pacifico, Active = true, X = 1.4f, Y = 1.5f };
        var paranoico = new OverworldEnemyModel { EnemyId = "ally_paranoico", BehaviorKind = OverworldBehavior.Paranoico, Active = true, X = 1.5f, Y = 1.6f };
        var normaleAlly = new OverworldEnemyModel { EnemyId = "ally_normale", BehaviorKind = OverworldBehavior.Normale, Active = true, X = 1.5f, Y = 1.4f };
        var others = new List<OverworldEnemyModel> { alerta, pacifico, paranoico, normaleAlly };

        // Uses the pure selection helper (not Build/ToBattleCharacter, which
        // reaches into the Database autoload and can't run outside a live
        // Godot engine).
        var reinforcements = OverworldEncounterPlanner.SelectReinforcements(enemy, others, reinforcementRadius: 10f);
        var pulledIds = new List<string>();
        foreach (var pulled in reinforcements)
        {
            pulledIds.Add(pulled.EnemyId);
        }

        assert.True(pulledIds.Contains("ally_allerta"), "allerta should be pulled in as reinforcement");
        assert.True(pulledIds.Contains("ally_normale"), "normale should be pulled in as reinforcement");
        assert.True(!pulledIds.Contains("ally_pacifico"), "pacifico should never be pulled in as reinforcement");
        assert.True(!pulledIds.Contains("ally_paranoico"), "paranoico should never be pulled in as reinforcement");
    }

    public static void ParanoicoPatrolsInsteadOfChasing(TestAssert assert)
    {
        var dungeon = MakeOpenDungeon(20, 5);
        dungeon.RoomBounds[1] = new Rect2I(0, 0, 5, 5);
        dungeon.RoomBounds[2] = new Rect2I(15, 0, 5, 5);

        var enemy = new OverworldEnemyModel
        {
            EnemyId = "para_1",
            BehaviorKind = OverworldBehavior.Paranoico,
            Intelligence = 7,
            Speed = 1.5f,
            X = 4f,
            Y = 2f,
        };

        // Player is far outside the dungeon bounds - no line of sight, no
        // lead - so the paranoico must fall back to patrolling a room.
        var decision = OverworldAI.ComputeVelocity(enemy, playerX: 5000f, playerY: 5000f, dungeon, alertTimer: 0f);
        assert.True(!decision.Chasing, "paranoico's own movement is never reported as a chase");
        assert.True(enemy.PatrolTargetRoomId is not null, "paranoico should pick a room to search when it has no lead");

        var firstTarget = enemy.PatrolTargetRoomId;
        // Room 1 (0,0,5x5) centers on grid (2,2) -> world (4,4); move within
        // arrival range of that exact point to simulate having walked there.
        enemy.X = 3.8f;
        enemy.Y = 3.7f;
        OverworldAI.ComputeVelocity(enemy, playerX: 5000f, playerY: 5000f, dungeon, alertTimer: 0f);
        assert.True(enemy.VisitedRoomIds.Contains(firstTarget!.Value), "arriving at the patrol target should mark it visited");
    }

    public static void ParanoicoAdoptsFreshestAllySighting(TestAssert assert)
    {
        var paranoico = new OverworldEnemyModel { EnemyId = "para_ask", BehaviorKind = OverworldBehavior.Paranoico, Active = true, X = 0f, Y = 0f };
        var staleWitness = new OverworldEnemyModel
        {
            EnemyId = "stale",
            Active = true,
            X = 1f,
            Y = 0f,
            LastKnownPlayerPosition = new Vector2(9f, 9f),
            LastKnownPlayerAgeSec = 30f,
        };
        var freshWitness = new OverworldEnemyModel
        {
            EnemyId = "fresh",
            Active = true,
            X = 1f,
            Y = 1f,
            LastKnownPlayerPosition = new Vector2(3f, 4f),
            LastKnownPlayerAgeSec = 1f,
        };

        OverworldAI.ParanoidAskAround(paranoico, new List<OverworldEnemyModel> { staleWitness, freshWitness });

        assert.True(paranoico.LastKnownPlayerPosition == new Vector2(3f, 4f), "paranoico should adopt the freshest in-range sighting, ignoring the stale one");
    }

    public static void WakeStuckAlly(TestAssert assert)
    {
        var smart = new OverworldEnemyModel
        {
            EnemyId = "smart",
            Active = true,
            Intelligence = 7,
            X = 1f,
            Y = 1f,
        };
        var stuck = new OverworldEnemyModel
        {
            EnemyId = "stuck",
            Active = true,
            Intelligence = 2,
            IsStuck = true,
            StuckTimer = OverworldAI.StuckTimeSec,
            X = 2f,
            Y = 1f,
        };
        var enemies = new List<OverworldEnemyModel> { smart, stuck };
        var rng = new GameRng(12345);

        OverworldAI.MaybeWakeStuckAlly(smart, enemies, rng);

        assert.True(!stuck.IsStuck, "stuck ally should be released by smart enemy");
        assert.True(stuck.WakeTimer > 0f, "woken ally should receive wake timer");
        assert.True(!string.IsNullOrWhiteSpace(stuck.SpeechText), "woken ally should emit speech");
        assert.True(smart.WakeTimer > 0f, "smart enemy should receive wake cooldown");
    }

    public static void BreakableBlocksLineOfSight(TestAssert assert)
    {
        var blockedGrid = new[,]
        {
            { 0, 0, 0 },
            { 0, (int)TileType.Breakable, 0 },
            { 0, 0, 0 },
        };

        var visible = OverworldAI.HasLineOfSight(blockedGrid, startX: 0f, startY: 2f, endX: 4f, endY: 2f);
        assert.True(!visible, "intact breakable should block line of sight");
    }
}
