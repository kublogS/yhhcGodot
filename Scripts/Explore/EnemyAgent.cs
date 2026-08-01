using System;
using Godot;

public partial class EnemyAgent : CharacterBody3D
{
    public OverworldEnemyModel Model { get; private set; } = new();
    private static readonly EnemyAsciiBillboardFactory AsciiFactory = new();
    private static readonly EnemyModelVisualFactory ModelFactory = new();

    private readonly EnemyVisualAnimator _visualAnimator = new();
    private readonly EnemyWingFlapRig _wingFlapRig = new();
    private PlayerController _player = null!;
    private DungeonData _dungeon = null!;
    private OverworldEnemyRuntimeCoordinator? _runtimeCoordinator;
    private Node3D? _visualRoot;
    private Node3D? _visualPivot;
    private Node3D? _visualModelRoot;
    private float _alertTimer;
    private bool _useMovementFacing = true;

    public void Setup(
        OverworldEnemyModel model,
        PlayerController player,
        DungeonData dungeon,
        OverworldEnemyRuntimeCoordinator? runtimeCoordinator = null)
    {
        Model = model;
        _player = player;
        _dungeon = dungeon;
        _runtimeCoordinator = runtimeCoordinator;
        GlobalPosition = new Vector3(model.Position.X, 0.75f, model.Position.Y);
        ConfigureVisualProfile();
        ClearChildren();

        _visualRoot = ModelFactory.Create(Model.Sprite);
        if (_visualRoot is not null)
        {
            BindVisualHierarchy(_visualRoot);
            AddChild(_visualRoot);
            _wingFlapRig.Capture(_visualRoot);
        }
        else
        {
            AddFallbackVisual();
        }

        ConfigureAnimator();
        AddCollisionShape();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player is null || _dungeon is null || !Model.Active)
        {
            return;
        }

        var dt = (float)delta;
        var chasing = false;
        if (_runtimeCoordinator is null)
        {
            _alertTimer = Mathf.Max(0f, _alertTimer - dt);
            OverworldAI.TickEnemyRuntime(Model, dt);
            var decision = OverworldAI.ComputeVelocity(
                Model,
                _player.GlobalPosition.X,
                _player.GlobalPosition.Z,
                _dungeon.Grid,
                _alertTimer);
            Velocity = new Vector3(decision.VxPerSec, 0f, decision.VyPerSec);
            chasing = decision.Chasing;
        }
        else
        {
            var decision = _runtimeCoordinator.GetDecision(Model);
            Velocity = new Vector3(decision.VxPerSec, 0f, decision.VyPerSec);
            chasing = decision.Chasing;
        }

        var oldPos = GlobalPosition;
        MoveAndSlide();
        var moved = (GlobalPosition - oldPos).LengthSquared() > 0.00000025f;
        OverworldAI.MarkMoveResult(Model, moved, dt);
        UpdateVisualFacing(dt);
        _visualAnimator.Tick(dt, Velocity);
        _wingFlapRig.Tick(dt);

        Model.Position = new Vector2(GlobalPosition.X, GlobalPosition.Z);
        if (chasing)
        {
            _alertTimer = 1.5f;
        }
    }

    private void ConfigureVisualProfile()
    {
        _useMovementFacing = true;
        _visualAnimator.Reset();
        _wingFlapRig.Reset();
        if (!EnemyModelLibrary.TryResolve(Model.Sprite, out var profile))
        {
            return;
        }

        _useMovementFacing = profile.UseMovementFacing;
        if (profile.UseWingFlap)
        {
            _wingFlapRig.Configure(profile.WingFlapSpeedHz, profile.WingFlapAngleDeg);
        }
    }

    private void UpdateVisualFacing(float dt)
    {
        if (_visualRoot is null || !_useMovementFacing || dt <= 0f)
        {
            return;
        }

        var planar = new Vector2(Velocity.X, Velocity.Z);
        if (planar.LengthSquared() < 0.02f)
        {
            return;
        }

        var targetYaw = Mathf.Atan2(planar.X, planar.Y) + MathF.PI;
        var currentYaw = _visualRoot.Rotation.Y;
        var blend = Mathf.Clamp(dt * 6f, 0f, 1f);
        _visualRoot.Rotation = new Vector3(0f, Mathf.LerpAngle(currentYaw, targetYaw, blend), 0f);
    }

    private void ConfigureAnimator()
    {
        var profile = EnemyVisualAnimationProfiles.Resolve(Model.Sprite, Model.Name, Model.SpecName);
        _visualAnimator.Capture(_visualPivot, _visualModelRoot ?? _visualPivot, profile);
    }

    private void AddFallbackVisual()
    {
        var visualRoot = CreateVisualHierarchy();
        var spritePath = GameAssets.ResolveEnemySpritePath(Model.Sprite);
        var asciiBillboard = AsciiFactory.Create(spritePath);
        if (asciiBillboard is not null)
        {
            _visualModelRoot!.AddChild(asciiBillboard);
            AddChild(visualRoot);
            return;
        }

        _visualModelRoot!.AddChild(new MeshInstance3D
        {
            Mesh = new CapsuleMesh { Radius = 0.4f, Height = 1.2f },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = PythonColorPalette.Red },
        });
        AddChild(visualRoot);
    }

    private void AddCollisionShape()
    {
        AddChild(new CollisionShape3D
        {
            Shape = new CapsuleShape3D { Radius = 0.35f, Height = 1.2f },
            Position = new Vector3(0f, 0.75f, 0f),
        });
    }

    private void ClearChildren()
    {
        _visualRoot = null;
        _visualPivot = null;
        _visualModelRoot = null;
        foreach (var child in GetChildren())
        {
            if (child is Node node)
            {
                node.QueueFree();
            }
        }
    }

    private void BindVisualHierarchy(Node3D visualRoot)
    {
        _visualRoot = visualRoot;
        _visualPivot = visualRoot.GetNodeOrNull<Node3D>("VisualPivot");
        _visualModelRoot = visualRoot.GetNodeOrNull<Node3D>("VisualPivot/ImportedModel");
    }

    private Node3D CreateVisualHierarchy()
    {
        var visualRoot = new Node3D { Name = "VisualRoot" };
        var visualPivot = new Node3D { Name = "VisualPivot" };
        var visualModelRoot = new Node3D { Name = "ImportedModel" };
        visualRoot.AddChild(visualPivot);
        visualPivot.AddChild(visualModelRoot);
        BindVisualHierarchy(visualRoot);
        return visualRoot;
    }
}
