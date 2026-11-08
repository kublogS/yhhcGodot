using System.Collections.Generic;
using Godot;

// Exercises the real BattleRewardService / RunLifecycleService. Both used to
// take the GameSession Node directly, so their logic had no test coverage and
// the flow-test fakes carried hand-copied versions of it.
public static class RewardAndRunLifecycleTests
{
    public static void StealTokensComeFromKills(TestAssert assert)
    {
        var session = new FakeSession { State = TestStateFactory.CreateState(5) };
        session.State!.BattleKills = 3;

        var rewards = BattleRewardService.Finalize(session);

        assert.Equal(2, rewards.Tokens, "3 kills should grant 2 steal tokens ((kills+1)/2)");
        assert.Equal(2, session.State.BattleStealTokens, "finalize must store the steal tokens on the state");
    }

    public static void AddMoveFillsEmptySlotsThenRequestsReplace(TestAssert assert)
    {
        var session = new FakeSession { State = TestStateFactory.CreateState(6) };
        var state = session.State!;
        state.BattleStealTokens = 10;

        for (var i = 0; i < 4; i++)
        {
            var added = BattleRewardService.TryAddMoveToPlayer(session, new MoveModel { Name = $"Mossa{i}" });
            assert.True(added, $"move {i} should fill an empty slot");
        }

        assert.Equal(6, state.BattleStealTokens, "each learned move must consume one steal token");
        assert.True(!session.HasPendingMoveReplace, "no replace prompt while empty slots remain");

        var overflow = new MoveModel { Name = "MossaExtra" };
        var addedOverflow = BattleRewardService.TryAddMoveToPlayer(session, overflow);

        assert.True(!addedOverflow, "a full move list must not silently accept a sixth move");
        assert.True(session.HasPendingMoveReplace, "a full move list must request a replace choice");
        assert.True(ReferenceEquals(overflow, session.PendingMove), "the pending move must be the one offered");
        assert.Equal(6, state.BattleStealTokens, "a pending replace must not consume a token yet");
    }

    public static void ClearAfterRewardResetsPendingReplace(TestAssert assert)
    {
        var session = new FakeSession { State = TestStateFactory.CreateState(7) };
        session.HasPendingMoveReplace = true;
        session.PendingMove = new MoveModel { Name = "Pendente" };
        session.PendingReplaceIndex = 3;

        BattleRewardService.ClearAfterReward(session);

        assert.Equal(0, session.State!.Enemies.Count, "battle enemies must be cleared after reward");
        assert.True(!session.HasPendingMoveReplace && session.PendingMove is null, "pending replace must be cleared");
        assert.Equal(0, session.PendingReplaceIndex, "pending replace index must be reset");
    }

    public static void XpCurveIsStrictlyIncreasing(TestAssert assert)
    {
        assert.Equal(50, BattleRewardService.XpNeededForLevel(1), "level 1 xp requirement");
        var previous = BattleRewardService.XpNeededForLevel(1);
        for (var level = 2; level <= 30; level++)
        {
            var needed = BattleRewardService.XpNeededForLevel(level);
            assert.True(needed > previous, $"xp needed must grow at level {level} ({previous} -> {needed})");
            previous = needed;
        }
    }

    public static void RunCompletesAfterLastFloorAndReturnsToHub(TestAssert assert)
    {
        var session = new FakeSession { State = TestStateFactory.CreateState(8) };
        session.CurrentRun.Reset(2);

        RunLifecycleService.BeginOverworldRun(session);
        assert.Equal(RunLocation.Overworld, session.CurrentRun.Location, "begin run must enter the overworld");
        assert.Equal(0, session.CurrentRun.Floor, "begin run must start at floor 0");
        assert.True(session.CurrentDungeon is not null, "begin run must generate a dungeon");
        assert.Equal(1, session.DeepestFloor, "deepest floor tracks the first floor");

        var advanced = RunLifecycleService.TryAdvanceFloor(session);
        assert.True(advanced, "advancing from floor 0 of 2 must succeed");
        assert.Equal(1, session.CurrentRun.Floor, "floor index after first advance");
        assert.Equal(2, session.DeepestFloor, "deepest floor must follow the advance");

        var advancedPastEnd = RunLifecycleService.TryAdvanceFloor(session);
        assert.True(!advancedPastEnd, "advancing past the last floor completes the run");
        assert.Equal(1, session.LabyrinthCompletions, "run completion must be counted");
        assert.Equal(RunLocation.Indice, session.CurrentRun.Location, "completed run returns to the hub");
        assert.True(session.CurrentDungeon is null && session.OverworldEnemies.Count == 0, "hub return must drop dungeon runtime");
        assert.Equal(2, session.DeepestFloor, "deepest floor survives run completion");
    }

    private sealed class FakeSession : ISessionSnapshotHost
    {
        public GameState? State { get; set; }
        public int CurrentSlot { get; set; } = 1;
        public DungeonData? CurrentDungeon { get; set; }
        public List<OverworldEnemyModel> OverworldEnemies { get; set; } = new();
        public Vector3 PlayerWorldPosition { get; set; } = Vector3.Zero;
        public float PlayerYawRadians { get; set; }
        public string LastEncounterContext { get; set; } = string.Empty;
        public List<string> LastBattleLog { get; } = new();
        public bool HasPendingMoveReplace { get; set; }
        public MoveModel? PendingMove { get; set; }
        public int PendingReplaceIndex { get; set; }
        public int DeepestFloor { get; set; }
        public int LabyrinthCompletions { get; set; }
        public RuntimeRunState CurrentRun { get; } = new();
    }
}
