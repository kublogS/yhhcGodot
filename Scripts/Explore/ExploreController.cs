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

        if (GameSession.Instance.State is null)
        {
            SceneRouter.Instance.GoToMainMenu();
            return;
        }

        if (GameSession.Instance.CurrentDungeon is null)
        {
            GameSession.Instance.GenerateNewDungeon();
        }

        BuildSceneFromSession();
    }

    public override void _Process(double delta)
    {
        if (GameSession.Instance.State is null || GameSession.Instance.CurrentDungeon is null)
        {
            return;
        }

        GameSession.Instance.PlayerWorldPosition = _player.GlobalPosition;
        GameSession.Instance.PlayerYawRadians = _player.Rotation.Y;
        _hud.UpdateFromState(GameSession.Instance.State.Player, GameSession.Instance.OverworldEnemies.Count);
        _map.UpdateFromDungeon(GameSession.Instance.CurrentDungeon, _player.GlobalPosition, _enemyAgents);

        if (_pause.Visible || _manual.Visible)
        {
            return;
        }

        foreach (var agent in _enemyAgents)
        {
            if (!agent.Model.Active)
            {
                continue;
            }

            var dist = agent.GlobalPosition.DistanceTo(_player.GlobalPosition);
            if (dist < 1.2f)
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
            _map.Visible = !_map.Visible;
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
        DungeonLayoutTuner.EnsureComfortablePassages(dungeon);
        DungeonBuilder.Build(_dungeonRoot, dungeon);
        _player.ConfigureDungeonNavigation(dungeon, DungeonBuilder.TileSize);

        _player.GlobalPosition = session.PlayerWorldPosition == Vector3.Zero ? dungeon.PlayerSpawn + new Vector3(0, 1.2f, 0) : session.PlayerWorldPosition;
        _player.Rotation = new Vector3(0f, session.PlayerYawRadians, 0f);

        foreach (var child in _enemiesRoot.GetChildren())
        {
            if (child is Node node)
            {
                node.QueueFree();
            }
        }

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
        CreateManualPanel(dungeon);
    }

    private void CreateManualPanel(DungeonData dungeon)
    {
        _manualPanel?.QueueFree();
        _manualPanel = new Node3D { Name = "ManualPanel" };
        _manualPanel.Position = dungeon.PlayerSpawn + new Vector3(3.0f, 1.1f, 0.0f);
        AddChild(_manualPanel);

        var mesh = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(1.4f, 1.0f, 0.1f) },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.85f, 0.84f, 0.72f) },
        };
        _manualPanel.AddChild(mesh);
    }

    private bool CanOpenManualFromPanel()
    {
        if (_manualPanel is null)
        {
            return false;
        }

        return _player.GlobalPosition.DistanceTo(_manualPanel.GlobalPosition) <= 2.5f;
    }
}
