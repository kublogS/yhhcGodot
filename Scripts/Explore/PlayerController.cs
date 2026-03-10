using Godot;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float Speed = 6.0f;
    [Export] public float MouseSensitivity = 0.0025f;
    [Export] public string TorchPlaceholderScenePath = "res://Scenes/Props/TorchPlaceholder.tscn";

    private Camera3D _camera = null!;
    private float _pitch;
    private bool _lookEnabled = true;
    private bool _movementEnabled = true;
    private float _collisionRadius = 0.35f;
    private PlayerNavigationResolver? _navigationResolver;
    private Node3D? _torchRig;
    private OmniLight3D? _torchLight;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
        if (GetNodeOrNull<CollisionShape3D>("CollisionShape3D")?.Shape is CapsuleShape3D capsule)
        {
            _collisionRadius = capsule.Radius;
        }

        EnsureTorchRig();
        SetTorchEnabled(false);
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Input(InputEvent @event)
    {
        if (!_lookEnabled || @event is not InputEventMouseMotion motion)
        {
            return;
        }

        RotateY(-motion.Relative.X * MouseSensitivity);
        _pitch = Mathf.Clamp(_pitch - motion.Relative.Y * MouseSensitivity, -1.35f, 1.35f);
        _camera.Rotation = new Vector3(_pitch, 0f, 0f);
    }

    public override void _PhysicsProcess(double delta)
    {
        var dt = Mathf.Max((float)delta, 0.0001f);
        if (!_movementEnabled)
        {
            Velocity = Vector3.Zero;
            MoveAndSlide();
            UpdateTorchWallClipping();
            return;
        }

        var input = Vector3.Zero;
        if (Input.IsActionPressed("move_forward")) input -= Transform.Basis.Z;
        if (Input.IsActionPressed("move_backward")) input += Transform.Basis.Z;
        if (Input.IsActionPressed("move_left")) input -= Transform.Basis.X;
        if (Input.IsActionPressed("move_right")) input += Transform.Basis.X;

        input.Y = 0f;
        input = input.Normalized();
        var desiredVelocity = new Vector3(input.X * Speed, 0f, input.Z * Speed);
        if (_navigationResolver is null)
        {
            Velocity = desiredVelocity;
        }
        else
        {
            var desiredMotion = desiredVelocity * dt;
            var resolvedMotion = _navigationResolver.ResolveMotion(GlobalPosition, desiredMotion);
            Velocity = resolvedMotion / dt;
        }

        MoveAndSlide();
        UpdateTorchWallClipping();
    }

    public Vector3 CameraForward()
    {
        return -_camera.GlobalTransform.Basis.Z;
    }

    public Camera3D GetViewCamera()
    {
        return _camera;
    }

    public void SetLookEnabled(bool enabled)
    {
        _lookEnabled = enabled;
        Input.MouseMode = enabled ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
    }

    public void SetMovementEnabled(bool enabled)
    {
        _movementEnabled = enabled;
        if (!enabled)
        {
            Velocity = Vector3.Zero;
        }
    }

    public void SetTorchEnabled(bool enabled)
    {
        EnsureTorchRig();
        if (_torchRig is not null)
        {
            _torchRig.Visible = enabled;
        }

        if (_torchLight is not null)
        {
            _torchLight.Visible = enabled;
        }
    }

    public void ConfigureDungeonNavigation(DungeonData dungeon, float tileSize)
    {
        var wallInset = tileSize * 0.12f;
        _navigationResolver = new PlayerNavigationResolver(dungeon, tileSize, _collisionRadius, wallInset);
    }

    public void ClearNavigation()
    {
        _navigationResolver = null;
    }

    private void EnsureTorchRig()
    {
        if (_torchRig is not null)
        {
            return;
        }

        var rig = new Node3D
        {
            Name = "TorchRig",
            Position = TorchRestLocalPosition,
            RotationDegrees = new Vector3(-10f, -18f, 4f),
        };
        _camera.AddChild(rig);
        _torchRig = rig;

        if (ResourceLoader.Exists(TorchPlaceholderScenePath) && GD.Load<PackedScene>(TorchPlaceholderScenePath) is { } scene)
        {
            if (scene.Instantiate() is Node torchNode)
            {
                rig.AddChild(torchNode);
            }
        }

        _torchLight = new OmniLight3D
        {
            Name = "TorchLight",
            Position = new Vector3(0.02f, 0.18f, -0.02f),
            LightEnergy = 2.65f,
            OmniRange = 11f,
            LightColor = new Color(1f, 0.78f, 0.45f),
            ShadowEnabled = false,
            Visible = false,
        };
        rig.AddChild(_torchLight);
    }
}
