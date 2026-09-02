using Godot;

public partial class PlayerController
{
    private static readonly Vector3 TorchRestLocalPosition = new(0.28f, -0.78f, -0.62f);

    // A carried object swings a bit more than the head holding it, and
    // slightly out of phase - it trails rather than moving in perfect
    // lockstep with the head bob.
    private const float TorchSwayVerticalAmplitude = 0.035f;
    private const float TorchSwayHorizontalAmplitude = 0.05f;
    private const float TorchSwayPhaseOffset = 0.6f;

    private void UpdateTorchWallClipping()
    {
        if (_torchRig is null || !_torchRig.Visible || _camera is null)
        {
            return;
        }

        var origin = _camera.GlobalPosition;
        var desiredGlobal = _camera.ToGlobal(TorchRestLocalPosition);
        var query = PhysicsRayQueryParameters3D.Create(origin, desiredGlobal);
        query.CollideWithAreas = false;
        query.CollideWithBodies = true;
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
        var target = TorchRestLocalPosition;

        if (hit.Count > 0 && hit.ContainsKey("position"))
        {
            var hitPos = hit["position"].AsVector3();
            var maxDist = Mathf.Max(0.12f, origin.DistanceTo(hitPos) - 0.1f);
            var direction = (desiredGlobal - origin).Normalized();
            var safeWorld = origin + (direction * maxDist);
            target = _camera.ToLocal(safeWorld);
        }

        var swayVertical = Mathf.Sin(_bobTimer + TorchSwayPhaseOffset) * TorchSwayVerticalAmplitude * _bobIntensity;
        var swayHorizontal = Mathf.Cos((_bobTimer * 0.5f) + TorchSwayPhaseOffset) * TorchSwayHorizontalAmplitude * _bobIntensity;
        target += new Vector3(swayHorizontal, swayVertical, 0f);

        _torchRig.Position = _torchRig.Position.Lerp(target, 0.45f);
    }
}
