using Godot;

public partial class PlayerController : CharacterBody3D
{
    [Export] public float Speed = 6.0f;
    [Export] public float MouseSensitivity = 0.0025f;
    [Export] public string[] FootstepSoundPaths =
    {
        "res://Assets/audio/footsteps/footstep_00.ogg",
        "res://Assets/audio/footsteps/footstep_01.ogg",
        "res://Assets/audio/footsteps/footstep_02.ogg",
        "res://Assets/audio/footsteps/footstep_03.ogg",
        "res://Assets/audio/footsteps/footstep_04.ogg",
        "res://Assets/audio/footsteps/footstep_05.ogg",
    };

    // Head bob: vertical sways at the full frequency, horizontal at half of
    // it, tracing the small figure-eight a head actually follows while
    // walking - not just up/down. Tied to how fast the player is actually
    // moving (post-collision), not input, so bumping into a wall doesn't bob.
    // Updated in _Process (every rendered frame) rather than _PhysicsProcess
    // so it stays smooth regardless of the physics tick rate.
    private const float BobFrequency = 1.8f;
    private const float BobVerticalAmplitude = 0.07f;
    private const float BobHorizontalAmplitude = 0.045f;
    private const float BobBlendSpeed = 6f;
    // Speed=6 units/sec is closer to a jog than a walk, so the stride needs
    // to be a running-length stride (~1.5-1.6m) rather than a walking one
    // (~0.8m) - the shorter value produced ~7 steps/sec, which read as
    // galloping instead of running.
    private const float FootstepStrideLength = 1.6f;

    private Camera3D _camera = null!;
    private AudioStreamPlayer3D? _footstepPlayer;
    private readonly System.Collections.Generic.List<AudioStream> _footstepStreams = new();
    private int _lastFootstepIndex = -1;
    private float _pitch;
    private bool _lookEnabled = true;
    private bool _movementEnabled = true;
    private float _collisionRadius = 0.35f;
    private PlayerNavigationResolver? _navigationResolver;
    private Node3D? _torchRig;
    private OmniLight3D? _torchLight;
    private Vector3 _cameraRestPosition;
    private float _bobTimer;
    private float _bobIntensity;
    private float _strideDistance;
    private readonly TorchFlameAnimator _flameAnimator = new();

    public override void _Ready()
    {
        _camera = GetNode<Camera3D>("Camera3D");
        _cameraRestPosition = _camera.Position;
        if (GetNodeOrNull<CollisionShape3D>("CollisionShape3D")?.Shape is CapsuleShape3D capsule)
        {
            _collisionRadius = capsule.Radius;
        }

        EnsureTorchRig();
        SetTorchEnabled(false);
        Input.MouseMode = Input.MouseModeEnum.Captured;
        EnsureFootstepPlayer();
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
    }

    public override void _Process(double delta)
    {
        var dt = Mathf.Max((float)delta, 0.0001f);
        UpdateHeadBobAndFootsteps(dt);
        UpdateTorchWallClipping();
        _flameAnimator.Tick(dt);
    }

    private void UpdateHeadBobAndFootsteps(float dt)
    {
        // No IsOnFloor() check: dungeon floor tiles are visual-only meshes
        // with no collider (movement is resolved entirely by
        // PlayerNavigationResolver, not Godot physics/gravity), so the
        // player is never actually "on floor" per CharacterBody3D - that
        // would silently gate this off permanently.
        var planarSpeed = new Vector2(Velocity.X, Velocity.Z).Length();
        var isWalking = _movementEnabled && planarSpeed > 0.15f;

        _bobIntensity = Mathf.Lerp(_bobIntensity, isWalking ? 1f : 0f, dt * BobBlendSpeed);
        if (isWalking)
        {
            _bobTimer += dt * planarSpeed * BobFrequency;
        }

        var vertical = Mathf.Sin(_bobTimer) * BobVerticalAmplitude * _bobIntensity;
        var horizontal = Mathf.Cos(_bobTimer * 0.5f) * BobHorizontalAmplitude * _bobIntensity;
        _camera.Position = _cameraRestPosition + new Vector3(horizontal, vertical, 0f);

        if (!isWalking)
        {
            _strideDistance = 0f;
            return;
        }

        _strideDistance += planarSpeed * dt;
        if (_strideDistance < FootstepStrideLength)
        {
            return;
        }

        _strideDistance -= FootstepStrideLength;
        PlayFootstep();
    }

    private void PlayFootstep()
    {
        if (_footstepPlayer is null || _footstepStreams.Count == 0)
        {
            return;
        }

        // Pick a different sample than last time (a handful of clean single
        // steps cycled without immediate repeats, instead of one sample
        // looping) plus a small random pitch/gain nudge per step.
        var index = _footstepStreams.Count == 1 ? 0 : GD.RandRange(0, _footstepStreams.Count - 2);
        if (index >= _lastFootstepIndex && _footstepStreams.Count > 1)
        {
            index++;
        }

        _lastFootstepIndex = index;
        _footstepPlayer.Stream = _footstepStreams[index];
        // Pitched down rather than up: a lower pitch reads as softer/rounder,
        // a higher one as sharper/clickier.
        _footstepPlayer.PitchScale = (float)GD.RandRange(0.82, 0.96);
        _footstepPlayer.VolumeDb = -12f + (float)GD.RandRange(-2.0, 2.0);
        _footstepPlayer.Play();
    }

    private const string FootstepBusName = "Footsteps";

    private void EnsureFootstepPlayer()
    {
        if (_footstepPlayer is not null)
        {
            return;
        }

        EnsureFootstepBus();
        var player = new AudioStreamPlayer3D
        {
            Name = "FootstepPlayer",
            MaxDistance = 12f,
            UnitSize = 3f,
            Bus = FootstepBusName,
        };
        AddChild(player);
        _footstepPlayer = player;

        _footstepStreams.Clear();
        foreach (var path in FootstepSoundPaths)
        {
            if (ResourceLoader.Exists(path) && GD.Load<AudioStream>(path) is { } stream)
            {
                _footstepStreams.Add(stream);
            }
        }

        if (_footstepStreams.Count == 0)
        {
            GD.PrintErr("[Player] Nessun suono di passi trovato.");
        }
    }

    private static void EnsureFootstepBus()
    {
        for (var i = 0; i < AudioServer.BusCount; i++)
        {
            if (AudioServer.GetBusName(i) == FootstepBusName)
            {
                return;
            }
        }

        var index = AudioServer.BusCount;
        AudioServer.AddBus(index);
        AudioServer.SetBusName(index, FootstepBusName);
        AudioServer.SetBusSend(index, "Master");
        // Low-pass filter shaves off the sharp high-frequency transient of
        // the raw sample, giving footsteps a duller/softer "thud" character
        // instead of a crisp, clicky one.
        AudioServer.AddBusEffect(index, new AudioEffectLowPassFilter { CutoffHz = 2200f });
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
        var model = TorchModelFactory.Create();
        rig.AddChild(model);

        _torchLight = new OmniLight3D
        {
            Name = "TorchLight",
            Position = new Vector3(0.02f, 0.95f, -0.02f),
            LightEnergy = 2.65f,
            OmniRange = 11f,
            LightColor = new Color(1f, 0.78f, 0.45f),
            ShadowEnabled = false,
            Visible = false,
        };
        rig.AddChild(_torchLight);
        _flameAnimator.Capture(_torchLight);
    }
}
