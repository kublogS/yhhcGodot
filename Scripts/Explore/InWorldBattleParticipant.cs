using Godot;

public sealed class InWorldBattleParticipant
{
    private readonly EnemyVisualAnimator _animator = new();
    private readonly EnemyWingFlapRig _wingFlapRig = new();
    private readonly MeshInstance3D? _targetRing;
    private Vector3 _lastPosition;
    private bool _selected;

    public InWorldBattleParticipant(
        string actorId,
        Node3D root,
        Node3D motionRoot,
        Node3D scaleRoot,
        MeshInstance3D? targetRing,
        EnemyVisualAnimationProfile profile,
        bool useWingFlap,
        float wingFlapSpeedHz,
        float wingFlapAngleDeg)
    {
        ActorId = actorId;
        Root = root;
        MotionRoot = motionRoot;
        ScaleRoot = scaleRoot;
        _targetRing = targetRing;
        _animator.Capture(MotionRoot, ScaleRoot, profile);
        if (useWingFlap)
        {
            _wingFlapRig.Configure(wingFlapSpeedHz, wingFlapAngleDeg);
            _wingFlapRig.Capture(root);
        }

        SnapToAnchor(root.GlobalPosition, root.Rotation.Y);
        SetSelected(false);
    }

    public string ActorId { get; }
    public Node3D Root { get; }
    public Node3D MotionRoot { get; }
    public Node3D ScaleRoot { get; }
    public Vector3 AnchorPosition { get; private set; }
    public float AnchorYaw { get; private set; }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        if (_targetRing is not null)
        {
            _targetRing.Visible = selected;
        }
    }

    public void SnapToAnchor(Vector3 anchorPosition, float anchorYaw)
    {
        AnchorPosition = anchorPosition;
        AnchorYaw = anchorYaw;
        Root.GlobalPosition = AnchorPosition;
        Root.Rotation = new Vector3(0f, AnchorYaw, 0f);
        _lastPosition = Root.GlobalPosition;
    }

    public void FacePoint(Vector3 worldPoint)
    {
        var planar = worldPoint - Root.GlobalPosition;
        planar.Y = 0f;
        if (planar.LengthSquared() < 0.001f)
        {
            return;
        }

        Root.Rotation = new Vector3(0f, Mathf.Atan2(planar.X, planar.Z), 0f);
    }

    public void Tick(float dt)
    {
        if (dt <= 0f || !Root.Visible)
        {
            return;
        }

        var velocity = (Root.GlobalPosition - _lastPosition) / dt;
        _lastPosition = Root.GlobalPosition;
        _animator.Tick(dt, velocity);
        _wingFlapRig.Tick(dt);
        if (_targetRing is not null && _selected)
        {
            var bob = 1f + (Mathf.Sin(Time.GetTicksMsec() * 0.008f) * 0.08f);
            _targetRing.Scale = new Vector3(bob, 1f, bob);
        }
    }

    public void SetVisible(bool visible)
    {
        Root.Visible = visible;
        if (_targetRing is not null)
        {
            _targetRing.Visible = visible && _selected;
        }
    }
}
