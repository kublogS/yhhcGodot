using System;
using System.Collections.Generic;

public static class SaveFlowFailureTests
{
    public static void NewGameSaveFailureBlocksTransition(TestAssert assert)
    {
        var session = new FakeNewGameSession { CurrentSlot = 2, State = TestStateFactory.CreateState(11) };
        session.CurrentRun.Apply(RunStatePolicy.EnterOverworld(777, 2, 20));
        var originalState = session.State;
        var originalRun = session.CurrentRun.ToContract();
        var flow = new NewGameFlowCoordinator(session, new FakePersistenceService(saveSuccess: false));

        var result = flow.StartNewGame("Hero");

        assert.True(!result.Success, "new game should fail when initial save fails");
        assert.Equal(SceneRoute.NameEntry, result.Route, "new game failure must stay in name entry");
        assert.True(ReferenceEquals(originalState, session.State), "failed new game must restore prior session state");
        assert.Equal(originalRun.ProceduralSeed, session.CurrentRun.Seed, "failed new game must restore prior run seed");
    }

    public static void RewardPersistFailureLeavesRecoverableState(TestAssert assert)
    {
        var session = new FakeRewardSession { CurrentSlot = 1, State = TestStateFactory.CreateState(17) };
        session.State!.Player.Exp = 200;
        session.State.Enemies = new List<CharacterModel> { TestStateFactory.CreateState(18).Enemies[0] };
        var flow = new RewardFlowCoordinator(session, new FakePersistenceService(saveSuccess: false));

        var result = flow.FinishAndPersist();

        assert.True(!result.Success, "reward continue should fail when persistence fails");
        assert.True(result.Recoverable, "reward failure should be marked recoverable");
        assert.Equal(SceneRoute.Reward, result.Route, "reward failure must keep the player on reward screen");
        assert.Equal(1, session.State!.Player.Level, "failed reward persist must rollback level-up mutation");
        assert.Equal(1, session.State.Enemies.Count, "failed reward persist must rollback battle cleanup");
    }

    public static void PurgeFailureDoesNotPretendSuccess(TestAssert assert)
    {
        var payload = SaveSlotRepository.DefaultSlot(1);
        payload.Meta.IsUsed = true;
        SaveRuntimeSnapshotMapper.ApplyRunState(payload.Runtime, RunStatePolicy.EnterOverworld(555, 3, 20));
        var store = new FakeSlotStore(payload, writeSuccess: false);
        var purger = new SaveRunSnapshotPurger();

        var result = purger.Purge(store, 555);

        assert.True(!result.Success, "purge failure must not be reported as success");
        assert.True(result.Recoverable, "purge failure should be recoverable");
        assert.True(!result.PersistenceChanged, "failed purge must not claim persistence changed when nothing was written");
    }

    public static void CorruptedSlotFallsBackSafely(TestAssert assert)
    {
        var payload = SaveSlotRepository.DeserializeOrDefault("null", 1, "null-runtime-test");
        assert.True(!payload.Meta.IsUsed, "corrupted slot must fallback to default unused payload");
        assert.Equal(1, payload.Meta.SlotId, "fallback slot id must stay aligned");
        assert.True(payload.Runtime.Run is not null, "fallback payload must hydrate canonical run state");
    }

    private sealed class FakePersistenceService : ISaveFlowPersistence
    {
        private readonly bool _saveSuccess;

        public FakePersistenceService(bool saveSuccess)
        {
            _saveSuccess = saveSuccess;
        }

        public bool SaveToSlot(int slotId) => _saveSuccess;

        public PersistResult PurgeRunSnapshots(int proceduralSeed)
        {
            return PersistResult.Succeeded(SceneRoute.None, persistenceChanged: false);
        }
    }

    private sealed class FakeSlotStore : ISaveSlotStore
    {
        private readonly SaveFileData _payload;
        private readonly bool _writeSuccess;

        public FakeSlotStore(SaveFileData payload, bool writeSuccess)
        {
            _payload = payload;
            _writeSuccess = writeSuccess;
        }

        public void EnsureSlotsExist() { }

        public SaveFileData ReadSanitized(int slotId)
        {
            return slotId == _payload.Meta.SlotId ? _payload : SaveSlotRepository.DefaultSlot(slotId);
        }

        public bool WriteSlot(int slotId, SaveFileData payload) => _writeSuccess;
    }

    private sealed class FakeNewGameSession : FakeSessionBase, INewGameSessionContext
    {
        public void CreateNewGame(string playerName)
        {
            State = TestStateFactory.CreateState(27);
            State.Player.Name = playerName;
            CurrentRun.Reset(20);
        }
    }

    private sealed class FakeRewardSession : FakeSessionBase, IRewardSessionContext
    {
        // Real reward logic; only move-stealing is stubbed so flow tests stay
        // independent of the enemy move pool.
        public (int Exp, int Soli, int Tokens, int Items) FinalizeBattleRewards() => BattleRewardService.Finalize(this);

        public void ClearAfterReward() => BattleRewardService.ClearAfterReward(this);

        public MoveModel? PickStealableMove() => null;

        public bool TryAddMoveToPlayer(MoveModel move) => false;

        public int XpNeededForLevel(int level) => BattleRewardService.XpNeededForLevel(level);
    }

    private abstract class FakeSessionBase : ISessionSnapshotHost
    {
        public GameState? State { get; set; }
        public int CurrentSlot { get; set; }
        public DungeonData? CurrentDungeon { get; set; }
        public List<OverworldEnemyModel> OverworldEnemies { get; set; } = new();
        public Godot.Vector3 PlayerWorldPosition { get; set; } = Godot.Vector3.Zero;
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
