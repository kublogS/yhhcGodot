using System.Collections.Generic;
using Godot;

public partial class ExploreController : Node3D
{
    private PlayerController _player = null!;
    private Node3D _dungeonRoot = null!;
    private Node3D _enemiesRoot = null!;
    private PauseOverlayController _pause = null!;
    private ManualOverlayController _manual = null!;
    private MapOverlayController _map = null!;
    private HudOverlayController _hud = null!;
    private Node3D _manualPanel = null!;
    private Vector3 _spawnDoorInteractPoint = Vector3.Zero;
    private float _spawnDoorInteractRadius = 2.5f;
    private bool _inSpawnHub;

    private readonly List<EnemyAgent> _enemyAgents = new();

    public override void _Ready()
    {
        _player = GetNode<PlayerController>("Player");
        _dungeonRoot = GetNode<Node3D>("DungeonRoot");
        _enemiesRoot = GetNode<Node3D>("EnemiesRoot");
        _pause = GetNode<PauseOverlayController>("CanvasLayer/PauseOverlay");
        _manual = GetNode<ManualOverlayController>("CanvasLayer/ManualOverlay");
        _map = GetNode<MapOverlayController>("CanvasLayer/MapOverlay");
        _hud = GetNode<HudOverlayController>("CanvasLayer/HudOverlay");
        ExploreLightingRig.Ensure(this);

        if (GameSession.Instance.State is null)
        {
            SceneRouter.Instance.GoToMainMenu();
            return;
        }

        if (GameSession.Instance.HasEnteredOverworld)
        {
            if (GameSession.Instance.CurrentDungeon is null)
            {
                GameSession.Instance.GenerateNewDungeon();
            }

            BuildSceneFromSession();
            return;
        }

        BuildSpawnHub();
    }

    public override void _Process(double delta)
    {
        var state = GameSession.Instance.State;
        if (state is null)
        {
            return;
        }

        GameSession.Instance.PlayerWorldPosition = _player.GlobalPosition;
        GameSession.Instance.PlayerYawRadians = _player.Rotation.Y;
        _hud.UpdateFromState(state.Player, _inSpawnHub ? 0 : GameSession.Instance.OverworldEnemies.Count);

        if (_inSpawnHub)
        {
            return;
        }

        var dungeon = GameSession.Instance.CurrentDungeon;
        if (dungeon is null)
        {
            return;
        }

        _map.UpdateFromDungeon(dungeon, _player.GlobalPosition, _enemyAgents);

        if (_pause.Visible || _manual.Visible)
        {
            return;
        }

        HandleOverworldExitTransition();

        foreach (var agent in _enemyAgents)
        {
            if (!agent.Model.Active)
            {
                continue;
            }

            if (agent.GlobalPosition.DistanceTo(_player.GlobalPosition) < 1.2f)
            {
                GameSession.Instance.StartEncounterWithEnemy(agent.Model);
                SceneRouter.Instance.GoToBattle();
                return;
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("toggle_pause"))
        {
            var open = !_pause.Visible;
            _pause.SetOverlayVisible(open);
            _player.SetLookEnabled(!open);
        }

        if (@event.IsActionPressed("toggle_map"))
        {
            if (!_inSpawnHub)
            {
                _map.Visible = !_map.Visible;
            }
        }

        if (@event.IsActionPressed("world_interact"))
        {
            HandleWorldInteractInput();
        }

        if (@event.IsActionPressed("toggle_manual"))
        {
            if (_manual.Visible)
            {
                _manual.SetOpen(false);
                _player.SetLookEnabled(!_pause.Visible);
            }
            else if (CanOpenManualFromPanel())
            {
                _manual.SetOpen(true);
                _player.SetLookEnabled(false);
            }
        }

        if (@event.IsActionPressed("ui_cancel") && _pause.Visible)
        {
            _pause.SetOverlayVisible(false);
            _player.SetLookEnabled(true);
        }
    }

    private void BuildSceneFromSession()
    {
        var session = GameSession.Instance;
        var dungeon = session.CurrentDungeon!;
        _inSpawnHub = false;
        _exitTransitionLock = false;
        DungeonLayoutTuner.EnsureComfortablePassages(dungeon);
        DungeonBuilder.Build(_dungeonRoot, dungeon);
        _player.ConfigureDungeonNavigation(dungeon, DungeonBuilder.TileSize);

        _player.GlobalPosition = session.PlayerWorldPosition == Vector3.Zero ? dungeon.PlayerSpawn + new Vector3(0, 1.2f, 0) : session.PlayerWorldPosition;
        _player.Rotation = new Vector3(0f, session.PlayerYawRadians, 0f);

        ClearNodeChildren(_enemiesRoot);
        _enemyAgents.Clear();
        foreach (var enemy in session.OverworldEnemies)
        {
            var agent = new EnemyAgent();
            _enemiesRoot.AddChild(agent);
            agent.Setup(enemy, _player, dungeon);
            _enemyAgents.Add(agent);
        }

        _manual.LoadZone("INDICE");
        _manual.Visible = false;
        _map.Visible = false;
        _pause.Visible = false;
        CreateManualPanelFromDungeon(dungeon);
    }

    private bool CanOpenManualFromPanel()
    {
        return _manualPanel is not null && _player.GlobalPosition.DistanceTo(_manualPanel.GlobalPosition) <= 2.8f;
    }
}
