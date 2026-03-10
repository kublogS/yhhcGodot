using Godot;

public partial class PlayerController
{
    private static readonly Vector3 TorchRestLocalPosition = new(0.28f, -0.78f, -0.62f);

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

        _torchRig.Position = _torchRig.Position.Lerp(target, 0.45f);
    }
}
