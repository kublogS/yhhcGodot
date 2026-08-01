using System;
using System.Collections.Generic;
using Godot;

public sealed class EnemyWingFlapRig
{
    private readonly List<WingTarget> _targets = new();
    private float _phase;
    private float _speedHz;
    private float _angleRad;

    public void Reset()
    {
        _targets.Clear();
        _phase = 0f;
        _speedHz = 0f;
        _angleRad = 0f;
    }

    public void Configure(float speedHz, float angleDeg)
    {
        _speedHz = Mathf.Max(0.1f, speedHz);
        _angleRad = Mathf.DegToRad(Mathf.Clamp(angleDeg, 1f, 70f));
    }

    public void Capture(Node? root)
    {
        _targets.Clear();
        if (root is null)
        {
            return;
        }

        CaptureRecursive(root);
    }

    public void Tick(float dt)
    {
        if (_targets.Count == 0 || _speedHz <= 0f || _angleRad <= 0f)
        {
            return;
        }

        _phase += dt * Mathf.Tau * _speedHz;
        var wingAngle = MathF.Sin(_phase) * _angleRad;
        foreach (var wing in _targets)
        {
            if (!GodotObject.IsInstanceValid(wing.Node))
            {
                continue;
            }

            wing.Node.Rotation = new Vector3(
                wing.BaseRotation.X,
                wing.BaseRotation.Y,
                wing.BaseRotation.Z + (wingAngle * wing.Sign));
        }
    }

    private void CaptureRecursive(Node node)
    {
        if (node is Node3D node3D && IsWingNodeName(node3D.Name))
        {
            var sign = IsLeftWing(node3D.Name) ? 1f : -1f;
            _targets.Add(new WingTarget(node3D, node3D.Rotation, sign));
        }

        foreach (var child in node.GetChildren())
        {
            if (child is Node childNode)
            {
                CaptureRecursive(childNode);
            }
        }
    }

    private static bool IsWingNodeName(StringName name)
    {
        var text = name.ToString().ToLowerInvariant();
        return text.Contains("wing") || text.Contains("ala");
    }

    private static bool IsLeftWing(StringName name)
    {
        var text = name.ToString().ToLowerInvariant();
        return text.Contains("left") || text.Contains("_l") || text.EndsWith("l");
    }

    private readonly struct WingTarget
    {
        public WingTarget(Node3D node, Vector3 baseRotation, float sign)
        {
            Node = node;
            BaseRotation = baseRotation;
            Sign = sign;
        }

        public Node3D Node { get; }
        public Vector3 BaseRotation { get; }
        public float Sign { get; }
    }
}
