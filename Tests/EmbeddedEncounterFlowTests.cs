using System;
using System.Collections.Generic;
using Godot;

public static class EmbeddedEncounterFlowTests
{
    public static void BattleFleeReturnsToExplore(TestAssert assert)
    {
        var request = new CombatTurnRequest { ActionType = CombatActionType.Flee };
        for (var seed = 1; seed <= 128; seed++)
        {
            var session = new FakeBattleSession { State = TestStateFactory.CreateState(seed) };
            var result = new BattleFlowCoordinator(session).ExecuteTurn(request);
            if (result.Outcome != BattleFlowOutcome.ReturnToExplore)
            {
                continue;
            }

            assert.Equal(BattleFlowOutcome.ReturnToExplore, result.Outcome, "successful flee should return to explore mode");
            return;
        }

        assert.True(false, "expected at least one deterministic flee success in test seed range");
    }

    public static void BattleVictoryOpensReward(TestAssert assert)
    {
        var session = new FakeBattleSession { State = TestStateFactory.CreateState(41) };
        session.State!.Enemies[0].Hp = 1;
        var result = new BattleFlowCoordinator(session).ExecuteTurn(new CombatTurnRequest
        {
            ActionType = CombatActionType.Attack,
            SelectedMoveIndex = 0,
            SelectedTargetIndex = 0,
        });

        assert.Equal(BattleFlowOutcome.OpenReward, result.Outcome, "battle victory should open reward overlay");
    }

    public static void RewardPersistSuccessReturnsExplore(TestAssert assert)
    {
        var session = new FakeRewardSession { CurrentSlot = 1, State = TestStateFactory.CreateState(23) };
        var flow = new RewardFlowCoordinator(session, new FakePersistenceService(saveSuccess: true));

        var result = flow.FinishAndPersist();

        assert.True(result.Success, "reward persist should succeed");
        assert.Equal(SceneRoute.Explore, result.Route, "successful reward persist should return to explore mode");
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

    private sealed class FakeBattleSession : IBattleSessionContext
    {
        public GameState? State { get; set; }

        public (int Exp, int Soli, int Tokens, int Items) FinalizeBattleRewards()
        {
            return (State?.BattleLootExp ?? 0, State?.BattleClaimableSoli ?? 0, State?.BattleStealTokens ?? 0, State?.BattleLootItems.Count ?? 0);
        }
    }

    private sealed class FakeRewardSession : IRewardSessionContext
    {
        public GameState? State { get; set; }
        public int CurrentSlot { get; set; }
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

        public (int Exp, int Soli, int Tokens, int Items) FinalizeBattleRewards()
        {
            return (State?.BattleLootExp ?? 0, State?.BattleClaimableSoli ?? 0, State?.BattleStealTokens ?? 0, State?.BattleLootItems.Count ?? 0);
        }

        public void ClearAfterReward()
        {
            if (State is null)
            {
                return;
            }

            State.Enemies.Clear();
            State.EnemyQueue.Clear();
            State.ResetBattleInstance();
            PendingMove = null;
            HasPendingMoveReplace = false;
            PendingReplaceIndex = 0;
        }

        public MoveModel? PickStealableMove() => null;

        public bool TryAddMoveToPlayer(MoveModel move) => false;

        public int XpNeededForLevel(int level)
        {
            var mult = 0.5f + 0.2f * Math.Max(0, level - 1);
            return Math.Max(1, (int)MathF.Round(100f * mult));
        }
    }
}
