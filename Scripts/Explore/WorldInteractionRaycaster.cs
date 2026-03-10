using Godot;

public static class WorldInteractionRaycaster
{
    public static Node? Raycast(Camera3D camera, float maxDistance)
    {
        if (!GodotObject.IsInstanceValid(camera))
        {
            return null;
        }

        var from = camera.GlobalTransform.Origin;
        var to = from + (-camera.GlobalTransform.Basis.Z * maxDistance);
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollideWithAreas = true;
        query.CollideWithBodies = true;

        var result = camera.GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (result.Count == 0 || !result.TryGetValue("collider", out var colliderVariant))
        {
            return null;
        }

        return colliderVariant.AsGodotObject() as Node;
    }

    public static bool HasGroupInHierarchy(Node? node, string groupName)
    {
        while (node is not null)
        {
            if (node.IsInGroup(groupName))
            {
                return true;
            }

            node = node.GetParent();
        }

        return false;
    }
}
