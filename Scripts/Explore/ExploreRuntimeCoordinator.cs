using System.Collections.Generic;
using Godot;

public sealed class ExploreRuntimeCoordinator
{
    private readonly SceneTree _tree;
    private readonly Node3D _owner;
    private readonly GameSession _session;
    private readonly PlayerController _player;
    private readonly PauseOverlayController _pause;
    private readonly ManualOverlayController _manual;
    private readonly MapOverlayController _map;
    private readonly HudOverlayController _hud;
    private readonly ExploreRuntimeSceneBuilder _sceneBuilder;
    private readonly ExploreTransitionFlow _transitions;
    private readonly ExploreInteractionCoordinator _interactionCoordinator;
    private readonly ExploreEncounterTrigger _encounterTrigger;
    private readonly ExploreEncounterModeController _encounterMode;
    private readonly List<EnemyAgent> _enemyAgents = new();
    private OverworldEnemyRuntimeCoordinator? _enemyRuntime;
    private Node3D? _manualPanel;
    private bool _inSpawnHub;

    public ExploreRuntimeCoordinator(
        SceneTree tree,
        Node3D owner,
        GameSession session,
        SaveService saveService,
        PlayerController player,
        Node3D dungeonRoot,
        Node3D enemiesRoot,
        PauseOverlayController pause,
        ManualOverlayController manual,
        MapOverlayController map,
        HudOverlayController hud,
        BattleController battleOverlay,
        RewardController rewardOverlay,
        InWorldBattlePresentationController battlePresentation,
        ExploreSceneAssembler sceneAssembler,
        ExploreWorldInteractionService interactions,
        ExploreTransitionFlow transitions)
    {
        _tree = tree;
        _owner = owner;
        _session = session;
        _player = player;
        _pause = pause;
        _manual = manual;
        _map = map;
        _hud = hud;
        _sceneBuilder = new ExploreRuntimeSceneBuilder(session, sceneAssembler, player, dungeonRoot, enemiesRoot, pause, manual, map, owner);
        _transitions = transitions;
        _encounterTrigger = new ExploreEncounterTrigger(owner, session);
        _encounterMode = new ExploreEncounterModeController(
            tree,
            session,
            player,
            pause,
            manual,
            map,
            hud,
            battleOverlay,
            rewardOverlay,
            battlePresentation,
            SyncEncounterWorldAfterEncounter);
        _interactionCoordinator = new ExploreInteractionCoordinator(
            tree,
            session,
            saveService,
            player,
            dungeonRoot,
            pause,
            manual,
            map,
            hud,
            interactions,
            () => _inSpawnHub,
            () => _manualPanel,
            BuildSpawnHubRuntime);
    }

    public void Initialize()
    {
        _pause.ResumeRequested += _interactionCoordinator.HandlePauseResume;
        _pause.ReturnToIndexRequested += _interactionCoordinator.HandlePauseReturnToIndex;
        _pause.MainMenuRequested += _interactionCoordinator.HandlePauseMainMenu;
        _encounterMode.Initialize();
        ExploreLightingRig.Ensure(_owner);
        if (_session.State is null)
        {
            SceneRouteNavigator.Navigate(SceneRoute.MainMenu, _tree);
            return;
        }

        if (_session.CurrentRun.Location == RunLocation.Overworld)
        {
            if (_session.CurrentDungeon is null)
            {
                _session.GenerateNewDungeon();
            }
            BuildDungeonRuntime();
            return;
        }
        BuildSpawnHubRuntime();
    }

    public void Tick(double deltaSeconds)
    {
        var state = _session.State;
        if (state is null)
        {
            return;
        }

        _session.PlayerWorldPosition = _player.GlobalPosition;
        _session.PlayerYawRadians = _player.Rotation.Y;
        _hud.UpdateFromState(state.Player, _inSpawnHub ? 0 : _session.OverworldEnemies.Count);
        if (_encounterMode.IsOverlayActive)
        {
            _encounterMode.Tick((float)deltaSeconds);
            return;
        }

        ExploreModalFlow.ApplyInputLock(_player, _pause, _manual, _map);
        if (_inSpawnHub)
        {
            _transitions.HandleSpawnPortalCrossing(_inSpawnHub, _player, _pause, _manual, _map, _session, BuildDungeonRuntime);
            return;
        }

        if (_session.CurrentDungeon is null)
        {
            return;
        }
        _enemyRuntime?.Tick((float)deltaSeconds, _player.GlobalPosition, _session.CurrentDungeon.Grid, _session.OverworldEnemies);
        _map.UpdateFromDungeon(_session.CurrentDungeon, _player.GlobalPosition, _enemyAgents);
        if (ExploreModalFlow.IsModalActive(_pause, _manual, _map))
        {
            return;
        }

        if (_transitions.TryHandleExitTransition(_session, _player, _enemyAgents, BuildDungeonRuntime, BuildSpawnHubRuntime))
        {
            return;
        }

        if (_encounterTrigger.TryStart(_player, _enemyAgents, _enemyRuntime))
        {
            _encounterMode.BeginEncounter(_enemyAgents);
        }
    }

    public void HandleInput(InputEvent @event)
    {
        if (!_encounterMode.IsOverlayActive) _interactionCoordinator.HandleInput(@event);
    }

    private void BuildSpawnHubRuntime()
    {
        _inSpawnHub = true;
        _enemyRuntime = null;
        _transitions.ExitTransitionLock = false;
        var build = _sceneBuilder.BuildSpawnHub(_manualPanel);
        var layout = build.SpawnHubLayout!;
        _manualPanel = build.ManualPanel;
        _enemyAgents.Clear();
        _transitions.SpawnPortalEntryMinZ = layout.PortalEntryMinZ;
        _transitions.SpawnPortalEntryHalfWidth = layout.PortalEntryHalfWidth;
        _transitions.SpawnPortalTransitionLock = false;
    }

    private void BuildDungeonRuntime()
    {
        if (_session.CurrentDungeon is null) return;
        _inSpawnHub = false;
        _transitions.ExitTransitionLock = false;
        var build = _sceneBuilder.BuildDungeon(_manualPanel);
        _manualPanel = build.ManualPanel;
        _enemyRuntime = build.EnemyRuntime;
        _enemyAgents.Clear();
        _enemyAgents.AddRange(build.EnemyAgents);
    }

    private void SyncEncounterWorldAfterEncounter()
    {
        if (_inSpawnHub) return;
        ExploreEncounterWorldSync.PruneConsumedAgents(_enemyAgents, _session.OverworldEnemies);
        ExploreEncounterWorldSync.RefreshEnemyRuntime(_enemyRuntime, _player, _session.CurrentDungeon, _session.OverworldEnemies);
    }
}
