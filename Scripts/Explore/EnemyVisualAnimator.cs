using System;
using Godot;

public sealed class EnemyVisualAnimator
{
    private Node3D? _motionRoot;
    private Node3D? _scaleRoot;
    private Vector3 _basePosition;
    private Vector3 _baseRotation;
    private Vector3 _baseScale = Vector3.One;
    private EnemyVisualAnimationProfile _profile;
    private float _time;
    private float _moveBlend;

    public void Reset()
    {
        _motionRoot = null;
        _scaleRoot = null;
        _time = 0f;
        _moveBlend = 0f;
        _basePosition = Vector3.Zero;
        _baseRotation = Vector3.Zero;
        _baseScale = Vector3.One;
    }

    public void Capture(Node3D? motionRoot, Node3D? scaleRoot, EnemyVisualAnimationProfile profile)
    {
        Reset();
        if (motionRoot is null)
        {
            return;
        }

        _motionRoot = motionRoot;
        _scaleRoot = scaleRoot ?? motionRoot;
        _profile = profile;
        _basePosition = motionRoot.Position;
        _baseRotation = motionRoot.Rotation;
        _baseScale = _scaleRoot.Scale;
    }

    public void Tick(float dt, Vector3 velocity)
    {
        if (_motionRoot is null || _scaleRoot is null || dt <= 0f)
        {
            return;
        }

        _time += dt;
        var planar = new Vector2(velocity.X, velocity.Z);
        var speed = planar.Length();
        var targetMove = speed >= _profile.MoveThreshold ? 1f : 0f;
        _moveBlend = Mathf.MoveToward(_moveBlend, targetMove, dt * _profile.BlendSpeed);
        var idleBlend = 1f - _moveBlend;

        var idlePhase = _time * Mathf.Tau * _profile.IdleBobSpeed;
        var movePhase = _time * Mathf.Tau * _profile.MoveBobSpeed;
        var idleWave = MathF.Sin(idlePhase);
        var moveWave = MathF.Sin(movePhase);
        var moveLift = _profile.SymmetricMoveBob ? moveWave : MathF.Max(0f, moveWave);

        var bob = (idleWave * _profile.IdleBobHeight * idleBlend) + (moveLift * _profile.MoveBobHeight * _moveBlend);
        var lateral = (MathF.Cos(idlePhase * 0.9f) * _profile.LateralSway * idleBlend)
            + (MathF.Sin(movePhase * 0.5f) * _profile.LateralSway * 0.55f * _moveBlend);
        _motionRoot.Position = _basePosition + new Vector3(lateral, bob, 0f);

        var speedFactor = _profile.MoveThreshold <= 0f
            ? 1f
            : Mathf.Clamp(speed / (_profile.MoveThreshold * 6f), 0f, 1f);
        var pitchDeg = (-_profile.PitchDegrees * speedFactor * _moveBlend)
            + (MathF.Sin(idlePhase * 0.5f) * _profile.PitchDegrees * 0.18f * idleBlend);
        var rollDeg = (moveWave * _profile.RollDegrees * _moveBlend)
            + (MathF.Sin(idlePhase * 0.7f) * _profile.RollDegrees * 0.35f * idleBlend);
        var yawSwayDeg = MathF.Sin((idlePhase * 0.55f) + (movePhase * 0.18f))
            * _profile.YawSwayDegrees
            * Mathf.Lerp(1f, 0.35f, _moveBlend);

        _motionRoot.Rotation = _baseRotation + new Vector3(
            Mathf.DegToRad(pitchDeg),
            Mathf.DegToRad(yawSwayDeg),
            Mathf.DegToRad(rollDeg));

        var squashSignal = (idleWave * idleBlend * 0.55f) + (moveWave * _moveBlend);
        var yScale = Mathf.Max(0.55f, 1f + (_profile.SquashAmount * squashSignal));
        var xzScale = Mathf.Max(0.55f, 1f - ((_profile.SquashAmount * squashSignal) * 0.5f));
        _scaleRoot.Scale = new Vector3(_baseScale.X * xzScale, _baseScale.Y * yScale, _baseScale.Z * xzScale);
    }
}
