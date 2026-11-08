using Godot;

public sealed class ExploreInteractionCoordinator
{
    private readonly SceneTree _tree;
    private readonly GameSession _session;
    private readonly ISaveFlowPersistence _saveService;
    private readonly PlayerController _player;
    private readonly Node3D _dungeonRoot;
    private readonly PauseOverlayController _pause;
    private readonly ManualOverlayController _manual;
    private readonly MapOverlayController _map;
    private readonly HudOverlayController _hud;
    private readonly ExploreWorldInteractionService _interactions;
    private readonly System.Func<bool> _isInSpawnHub;
    private readonly System.Func<Node3D?> _manualPanelGetter;
    private readonly System.Action _buildSpawnHub;

    public ExploreInteractionCoordinator(
        SceneTree tree,
        GameSession session,
        ISaveFlowPersistence saveService,
        PlayerController player,
        Node3D dungeonRoot,
        PauseOverlayController pause,
        ManualOverlayController manual,
        MapOverlayController map,
        HudOverlayController hud,
        ExploreWorldInteractionService interactions,
        System.Func<bool> isInSpawnHub,
        System.Func<Node3D?> manualPanelGetter,
        System.Action buildSpawnHub)
    {
        _tree = tree;
        _session = session;
        _saveService = saveService;
        _player = player;
        _dungeonRoot = dungeonRoot;
        _pause = pause;
        _manual = manual;
        _map = map;
        _hud = hud;
        _interactions = interactions;
        _isInSpawnHub = isInSpawnHub;
        _manualPanelGetter = manualPanelGetter;
        _buildSpawnHub = buildSpawnHub;
    }

    public void HandleInput(InputEvent @event)
    {
        var isModalOpen = ExploreModalFlow.IsModalActive(_pause, _manual, _map);
        if (_interactions.IsManualClicked(@event, _player, isModalOpen))
        {
            ExploreModalFlow.OpenManual(_player, _pause, _manual, _map);
            return;
        }

        if (@event.IsActionPressed("ui_cancel") && ExploreModalFlow.HandleCancel(_player, _pause, _manual, _map))
        {
            return;
        }

        if (@event.IsActionPressed("toggle_pause"))
        {
            _pause.SetOverlayVisible(!_pause.Visible);
            ExploreModalFlow.ApplyInputLock(_player, _pause, _manual, _map);
        }

        if (@event.IsActionPressed("toggle_map") && !_isInSpawnHub())
        {
            _map.Visible = !_map.Visible;
            ExploreModalFlow.ApplyInputLock(_player, _pause, _manual, _map);
        }

        if (@event.IsActionPressed("world_interact"))
        {
            HandleWorldInteract();
        }

        if (@event.IsActionPressed("toggle_manual"))
        {
            if (_manual.Visible)
            {
                _manual.SetOpen(false);
                ExploreModalFlow.ApplyInputLock(_player, _pause, _manual, _map);
            }
            else if (ExploreModalFlow.CanOpenManualFromPanel(_player, _manualPanelGetter()))
            {
                ExploreModalFlow.OpenManual(_player, _pause, _manual, _map);
            }
        }
    }

    public void HandlePauseResume()
    {
        ExploreModalFlow.ApplyInputLock(_player, _pause, _manual, _map);
    }

    public void HandlePauseReturnToIndex()
    {
        var result = TryReturnToIndex();
        if (!result.Success)
        {
            _hud.ShowTransientStatus(result.Message, 3.4f);
            return;
        }

        _pause.SetOverlayVisible(false);
        _buildSpawnHub();
        _hud.ShowTransientStatus(result.Message);
    }

    public void HandlePauseMainMenu()
    {
        var result = TryLeaveToMainMenu();
        if (!result.Success)
        {
            _hud.ShowTransientStatus(result.Message, 3.4f);
            return;
        }

        _pause.SetOverlayVisible(false);
        SceneRouteNavigator.Navigate(result.Route, _tree);
    }

    private void HandleWorldInteract()
    {
        if (ExploreModalFlow.IsModalActive(_pause, _manual, _map))
        {
            return;
        }

        if (_isInSpawnHub())
        {
            if (ExploreModalFlow.CanOpenManualFromPanel(_player, _manualPanelGetter()))
            {
                ExploreModalFlow.OpenManual(_player, _pause, _manual, _map);
            }

            return;
        }

        if (_interactions.TryUseSaveSanctuary(_session, _saveService, _player, _hud, _isInSpawnHub()))
        {
            return;
        }

        _interactions.TryBreakFacingBreakable(_session, _player, _dungeonRoot);
    }

    private PersistResult TryReturnToIndex()
    {
        if (_session.CurrentRun.Seed <= 0)
        {
            return PersistResult.Succeeded(SceneRoute.None, persistenceChanged: false, message: "Ritorno all'Indice");
        }

        var result = _saveService.PurgeRunSnapshots(_session.CurrentRun.Seed);
        if (!result.Success)
        {
            return PersistResult.Failed(SceneRoute.None, result.Message, result.Recoverable, result.PersistenceChanged);
        }

        return PersistResult.Succeeded(SceneRoute.None, result.PersistenceChanged, "Run procedural resettata");
    }

    private PersistResult TryLeaveToMainMenu()
    {
        if (_session.CurrentRun.Seed <= 0)
        {
            return PersistResult.Succeeded(SceneRoute.MainMenu, persistenceChanged: false);
        }

        var result = _saveService.PurgeRunSnapshots(_session.CurrentRun.Seed);
        if (!result.Success)
        {
            return PersistResult.Failed(SceneRoute.None, result.Message, result.Recoverable, result.PersistenceChanged);
        }

        return PersistResult.Succeeded(SceneRoute.MainMenu, result.PersistenceChanged);
    }
}
