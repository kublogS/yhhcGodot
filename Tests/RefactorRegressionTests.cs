using System.Collections.Generic;
using System.Linq;
using Godot;

public static class RefactorRegressionTests
{
    public static int RunAll(string[]? filters = null)
    {
        var cases = new (string Name, System.Action<TestAssert> Run)[]
        {
            ("breakable_walkability_semantics", BreakableWalkabilitySemantics),
            ("run_state_contract_reset", RunStateContractReset),
            ("dungeon_seed_determinism", DungeonSeedDeterminism),
            ("combat_turn_determinism", CombatTurnDeterminism),
            ("save_runtime_roundtrip", SaveRuntimeRoundtrip),
            ("overworld_pacifico_never_engages", OverworldAiParityTests.PacificoNeverEngages),
            ("overworld_allerta_chases_only_while_visible", OverworldAiParityTests.AllertaChasesOnlyWhileVisible),
            ("overworld_normale_only_engages_as_reinforcement", OverworldAiParityTests.NormaleOnlyEngagesAsReinforcement),
            ("overworld_paranoico_patrols_instead_of_chasing", OverworldAiParityTests.ParanoicoPatrolsInsteadOfChasing),
            ("overworld_paranoico_adopts_freshest_ally_sighting", OverworldAiParityTests.ParanoicoAdoptsFreshestAllySighting),
            ("overworld_breakable_blocks_line_of_sight", OverworldAiParityTests.BreakableBlocksLineOfSight),
            ("overworld_wake_stuck_ally", OverworldAiParityTests.WakeStuckAlly),
            ("overworld_enemy_spawn_determinism", OverworldEnemySpawnTests.DeterministicBySeedAndFloor),
            ("overworld_one_paranoico_per_floor_and_allerta_majority", OverworldEnemySpawnTests.OneParanoicoPerFloorAndAllertaMajority),
            ("enemy_intel_converges_on_repeated_weakness_signal", EnemyPlayerIntelTests.ConvergesOnRepeatedWeaknessSignal),
            ("enemy_intel_nullify_is_instant_reveal", EnemyPlayerIntelTests.NullifyIsAnInstantReveal),
            ("enemy_intel_neutral_hits_do_not_converge", EnemyPlayerIntelTests.NeutralHitsDoNotFalselyConverge),
            ("new_game_save_failure_blocks_transition", SaveFlowFailureTests.NewGameSaveFailureBlocksTransition),
            ("reward_persist_failure_recoverable", SaveFlowFailureTests.RewardPersistFailureLeavesRecoverableState),
            ("purge_failure_not_silent_success", SaveFlowFailureTests.PurgeFailureDoesNotPretendSuccess),
            ("corrupted_slot_falls_back_safely", SaveFlowFailureTests.CorruptedSlotFallsBackSafely),
            ("battle_flee_returns_to_explore", EmbeddedEncounterFlowTests.BattleFleeReturnsToExplore),
            ("battle_victory_opens_reward", EmbeddedEncounterFlowTests.BattleVictoryOpensReward),
            ("reward_persist_success_returns_explore", EmbeddedEncounterFlowTests.RewardPersistSuccessReturnsExplore),
        };
        var activeCases = filters is { Length: > 0 }
            ? cases.Where(test => Array.Exists(filters, filter => test.Name.Contains(filter, System.StringComparison.OrdinalIgnoreCase))).ToArray()
            : cases;

        var failures = 0;
        foreach (var (name, test) in activeCases)
        {
            var assert = new TestAssert();
            try
            {
                test(assert);
            }
            catch (System.Exception ex)
            {
                assert.True(false, $"Unhandled exception: {ex.GetType().Name}: {ex.Message}");
            }
            if (assert.FailureCount == 0)
            {
                System.Console.WriteLine($"[PASS] {name}");
                continue;
            }

            failures += assert.FailureCount;
            System.Console.WriteLine($"[FAIL] {name}");
            foreach (var failure in assert.Failures)
            {
                System.Console.WriteLine($"  - {failure}");
            }
        }

        return failures;
    }

    private static void BreakableWalkabilitySemantics(TestAssert assert)
    {
        var dungeon = BuildMinimalBreakableDungeon();
        assert.Equal(TileTraversalState.BreakableIntact, dungeon.GetTraversalState(1, 1), "breakable tile starts as intact");
        assert.True(!dungeon.IsWalkable(1, 1), "intact breakable must block movement");

        var broken = dungeon.TryBreakBreakable(1, 1);
        assert.True(broken, "breakable tile must be breakable");
        assert.Equal(TileTraversalState.BreakableBroken, dungeon.GetTraversalState(1, 1), "broken breakable state expected");
        assert.True(dungeon.IsWalkable(1, 1), "broken breakable must become walkable");
    }

    private static void RunStateContractReset(TestAssert assert)
    {
        var active = RunStatePolicy.EnterOverworld(proceduralSeed: 55, proceduralFloor: 0, maxFloors: 20);
        assert.True(active.IsRunActive, "new run contract must be active");
        assert.Equal(RunLocation.Overworld, active.Location, "new run location must be overworld");

        var advanced = RunStatePolicy.AdvanceFloor(active, out var completed);
        assert.True(!completed, "run should not complete at floor 1 when max is 20");
        assert.Equal(1, advanced.ProceduralFloor, "advance should increase floor");

        var reset = RunStatePolicy.ReturnToIndice(maxFloors: 20);
        assert.True(!reset.IsRunActive, "run must be inactive in INDICE");
        assert.Equal(RunLocation.Indice, reset.Location, "location must be INDICE");
        assert.Equal(0, reset.ProceduralSeed, "seed must be cleared in INDICE");
        assert.Equal(0, reset.ProceduralFloor, "floor must be cleared in INDICE");
    }

    private static void DungeonSeedDeterminism(TestAssert assert)
    {
        const int seed = 321654;
        const int floor = 4;
        var first = DungeonGenerator.Generate(seed, floor);
        var second = DungeonGenerator.Generate(seed, floor);

        assert.Equal(first.Width, second.Width, "width must be deterministic");
        assert.Equal(first.Height, second.Height, "height must be deterministic");
        for (var y = 0; y < first.Height; y++)
        {
            for (var x = 0; x < first.Width; x++)
            {
                assert.Equal(first.Grid[y, x], second.Grid[y, x], $"grid mismatch at {x},{y}");
            }
        }
    }

    private static void CombatTurnDeterminism(TestAssert assert)
    {
        var left = TestStateFactory.CreateState(77);
        var right = TestStateFactory.CreateState(77);
        var request = new CombatTurnRequest
        {
            ActionType = CombatActionType.Attack,
            SelectedMoveIndex = 0,
            SelectedTargetIndex = 0,
        };

        var leftOutcome = CombatService.CombatTurn(left, request);
        var rightOutcome = CombatService.CombatTurn(right, request);

        var leftEvents = leftOutcome.Events.Select(e => e.Message).ToList();
        var rightEvents = rightOutcome.Events.Select(e => e.Message).ToList();
        assert.SequenceEqual(leftEvents, rightEvents, "combat events must be deterministic with same seed");
        assert.Equal(left.Player.Hp, right.Player.Hp, "player HP must match deterministic outcome");
        assert.Equal(left.Enemies[0].Hp, right.Enemies[0].Hp, "enemy HP must match deterministic outcome");
    }

    private static void SaveRuntimeRoundtrip(TestAssert assert)
    {
        var source = BuildMinimalBreakableDungeon();
        source.TryBreakBreakable(1, 1);
        var savedWorld = DungeonRuntimeSnapshotMapper.ToSavedWorld(
            source,
            playerWorldPosition: new Vector3(0f, 1.2f, 0f),
            playerYaw: 0.3f,
            fallbackSeed: 445566);
        var reloaded = DungeonRuntimeSnapshotMapper.ToDungeon(savedWorld);

        assert.True(reloaded is not null, "saved runtime should rebuild a dungeon");
        assert.Equal(source.Width, reloaded!.Width, "roundtrip width mismatch");
        assert.Equal(source.Height, reloaded.Height, "roundtrip height mismatch");
        assert.True(reloaded.BrokenBreakableTiles.Contains(new Vector2I(1, 1)), "broken breakable must survive save/load");
    }

    private static DungeonData BuildMinimalBreakableDungeon()
    {
        var grid = new[,]
        {
            { (int)TileType.Wall, (int)TileType.Wall, (int)TileType.Wall },
            { (int)TileType.Wall, (int)TileType.Breakable, (int)TileType.Wall },
            { (int)TileType.Wall, (int)TileType.Floor, (int)TileType.Wall },
        };
        return new DungeonData
        {
            Grid = grid,
            RoomIdGrid = new int[3, 3],
            BreakableTiles = new HashSet<Vector2I> { new(1, 1) },
            PlayerSpawn = new Vector3(0f, 1.2f, 0f),
            Seed = 7,
            FloorIndex = 1,
        };
    }
}
