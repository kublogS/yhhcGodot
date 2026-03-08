using Godot;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float Speed = 6.0f;
    [Export] public float MouseSensitivity = 0.0025f;

    private Camera3D _camera = null!;
    private float _pitch;
    private bool _lookEnabled = true;
    private float _collisionRadius = 0.35f;
    private PlayerNavigationResolver? _navigationResolver;

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
        if (GetNodeOrNull<CollisionShape3D>("CollisionShape3D")?.Shape is CapsuleShape3D capsule)
        {
            _collisionRadius = capsule.Radius;
        }

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
    }

    public Vector3 CameraForward()
    {
        return -_camera.GlobalTransform.Basis.Z;
    }

    public void SetLookEnabled(bool enabled)
    {
        _lookEnabled = enabled;
        Input.MouseMode = enabled ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
    }

    public void ConfigureDungeonNavigation(DungeonData dungeon, float tileSize)
    {
        var wallInset = tileSize * 0.12f;
        _navigationResolver = new PlayerNavigationResolver(dungeon, tileSize, _collisionRadius, wallInset);
    }
}
