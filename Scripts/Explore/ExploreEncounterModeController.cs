using System;
using System.Collections.Generic;
using Godot;

public sealed class ExploreEncounterModeController
{
    private readonly SceneTree _tree;
    private readonly GameSession _session;
    private readonly PlayerController _player;
    private readonly PauseOverlayController _pause;
    private readonly ManualOverlayController _manual;
    private readonly MapOverlayController _map;
    private readonly HudOverlayController _hud;
    private readonly BattleController _battleOverlay;
    private readonly RewardController _rewardOverlay;
    private readonly InWorldBattlePresentationController _presentation;
    private readonly Action _syncEncounterWorld;

    public ExploreEncounterModeController(
        SceneTree tree,
        GameSession session,
        PlayerController player,
        PauseOverlayController pause,
        ManualOverlayController manual,
        MapOverlayController map,
        HudOverlayController hud,
        BattleController battleOverlay,
        RewardController rewardOverlay,
        InWorldBattlePresentationController presentation,
        Action syncEncounterWorld)
    {
        _tree = tree;
        _session = session;
        _player = player;
        _pause = pause;
        _manual = manual;
        _map = map;
        _hud = hud;
        _battleOverlay = battleOverlay;
        _rewardOverlay = rewardOverlay;
        _presentation = presentation;
        _syncEncounterWorld = syncEncounterWorld;
    }

    public bool IsOverlayActive => _battleOverlay.Visible || _rewardOverlay.Visible;

    public void Initialize()
    {
        _battleOverlay.OutcomeResolved += HandleBattleOutcome;
        _battleOverlay.TargetChanged += HandleTargetChanged;
        _battleOverlay.EventPresenter = battleEvent => _presentation.PresentEventAsync(battleEvent, _session.State);
        _rewardOverlay.PersistResolved += HandleRewardResult;
        _battleOverlay.CloseOverlay();
        _rewardOverlay.CloseOverlay();
    }

    public void Tick(float dt)
    {
        if (_presentation.IsActive)
        {
            _presentation.Tick(dt);
        }
    }

    public bool BeginEncounter(IReadOnlyList<EnemyAgent> enemyAgents)
    {
        ForceWorldFreeze();
        _presentation.BeginEncounter(_session, enemyAgents);
        if (_battleOverlay.OpenOverlay())
        {
            return true;
        }

        _session.RestoreEncounterWorldEnemies();
        _syncEncounterWorld();
        _session.ClearEncounterWorldEnemies();
        _presentation.EndEncounter();
        RestoreExploreControl();
        _hud.ShowTransientStatus("Encounter non disponibile.", 3f);
        SceneRouteNavigator.Navigate(SceneRoute.MainMenu, _tree);
        return false;
    }

    private void HandleBattleOutcome(BattleFlowOutcome outcome)
    {
        switch (outcome)
        {
            case BattleFlowOutcome.ReturnToExplore:
                _battleOverlay.CloseOverlay();
                _session.RestoreEncounterWorldEnemies();
                FinishEncounterAndResume(clearConsumed: false);
                break;
            case BattleFlowOutcome.OpenReward:
                _battleOverlay.CloseOverlay();
                if (!_rewardOverlay.OpenOverlay())
                {
                    FinishEncounterAndResume(clearConsumed: true);
                }
                break;
            case BattleFlowOutcome.GoToMainMenu:
                _battleOverlay.CloseOverlay();
                _rewardOverlay.CloseOverlay();
                _session.ClearEncounterWorldEnemies();
                _presentation.EndEncounter();
                SceneRouteNavigator.Navigate(SceneRoute.MainMenu, _tree);
                break;
        }
    }

    private void HandleRewardResult(PersistResult result)
    {
        if (!result.Success)
        {
            if (!string.IsNullOrWhiteSpace(result.Message))
            {
                _hud.ShowTransientStatus(result.Message, 3.4f);
            }
            return;
        }

        _rewardOverlay.CloseOverlay();
        if (result.Route == SceneRoute.MainMenu)
        {
            _session.ClearEncounterWorldEnemies();
            _presentation.EndEncounter();
            SceneRouteNavigator.Navigate(SceneRoute.MainMenu, _tree);
            return;
        }

        FinishEncounterAndResume(clearConsumed: true);
    }

    private void FinishEncounterAndResume(bool clearConsumed)
    {
        if (clearConsumed)
        {
            _session.ClearEncounterWorldEnemies();
        }

        _syncEncounterWorld();
        _session.ClearEncounterWorldEnemies();
        _presentation.EndEncounter();
        RestoreExploreControl();
    }

    private void ForceWorldFreeze()
    {
        _pause.SetOverlayVisible(false);
        _manual.SetOpen(false);
        _map.Visible = false;
        _player.SetLookEnabled(false);
        _player.SetMovementEnabled(false);
    }

    private void RestoreExploreControl()
    {
        _pause.SetOverlayVisible(false);
        _manual.SetOpen(false);
        _map.Visible = false;
        ExploreModalFlow.ApplyInputLock(_player, _pause, _manual, _map);
    }

    private void HandleTargetChanged(int selectedTarget)
    {
        _presentation.UpdateTargetSelection(_session.State, selectedTarget);
    }
}
