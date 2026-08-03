using System;
using System.Collections.Generic;
using Godot;

public readonly struct EnemyModelBinding
{
    public EnemyModelBinding(
        string scenePath,
        Vector3 scale,
        float heightOffset,
        float yawDegrees,
        bool playImportedAnimations = true,
        bool useMovementFacing = true,
        bool useWingFlap = false,
        float wingFlapAngleDeg = 24f,
        float wingFlapSpeedHz = 4f)
    {
        ScenePath = scenePath;
        Scale = scale;
        HeightOffset = heightOffset;
        YawDegrees = yawDegrees;
        PlayImportedAnimations = playImportedAnimations;
        UseMovementFacing = useMovementFacing;
        UseWingFlap = useWingFlap;
        WingFlapAngleDeg = wingFlapAngleDeg;
        WingFlapSpeedHz = wingFlapSpeedHz;
    }

    public string ScenePath { get; }
    public Vector3 Scale { get; }
    public float HeightOffset { get; }
    public float YawDegrees { get; }
    public bool PlayImportedAnimations { get; }
    public bool UseMovementFacing { get; }
    public bool UseWingFlap { get; }
    public float WingFlapAngleDeg { get; }
    public float WingFlapSpeedHz { get; }
}

public static class EnemyModelLibrary
{
    private const string Root = "res://Assets/characters/enemy3d/";

    private static readonly Dictionary<string, EnemyModelBinding> Bindings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["slime"] = new EnemyModelBinding(
            Root + "Slime.fbx",
            new Vector3(0.7f, 0.7f, 0.7f),
            -0.35f,
            180f,
            playImportedAnimations: false,
            useMovementFacing: true,
            useWingFlap: false),
        ["scheletro"] = new EnemyModelBinding(
            Root + "Skeleton.fbx",
            new Vector3(0.85f, 0.85f, 0.85f),
            -0.42f,
            180f,
            playImportedAnimations: false,
            useMovementFacing: true,
            useWingFlap: false),
        ["orco"] = new EnemyModelBinding(
            Root + "Dragon.fbx",
            new Vector3(0.36f, 0.36f, 0.36f),
            -0.35f,
            180f,
            playImportedAnimations: false,
            useMovementFacing: true,
            useWingFlap: true,
            wingFlapAngleDeg: 16f,
            wingFlapSpeedHz: 2.4f),
        ["goblin"] = new EnemyModelBinding(
            Root + "Bat.fbx",
            new Vector3(0.52f, 0.52f, 0.52f),
            -0.25f,
            180f,
            playImportedAnimations: false,
            useMovementFacing: true,
            useWingFlap: true,
            wingFlapAngleDeg: 34f,
            wingFlapSpeedHz: 5.2f),
    };

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["skeleton"] = "scheletro",
        ["orc"] = "orco",
        ["slime"] = "slime",
        ["goblin"] = "goblin",
    };

    public static bool TryResolve(string? spriteId, out EnemyModelBinding binding)
    {
        var key = NormalizeKey(spriteId);
        if (key.Length == 0)
        {
            binding = default;
            return false;
        }

        if (Aliases.TryGetValue(key, out var alias))
        {
            key = alias;
        }

        if (!Bindings.TryGetValue(key, out binding))
        {
            return false;
        }

        return ResourceLoader.Exists(binding.ScenePath);
    }

    public static EnemyModelBinding GetVisualProfileOrDefault(string? spriteId)
    {
        return TryResolve(spriteId, out var binding)
            ? binding
            : new EnemyModelBinding(string.Empty, Vector3.One, 0f, 0f);
    }

    private static string NormalizeKey(string? spriteId)
    {
        if (string.IsNullOrWhiteSpace(spriteId))
        {
            return string.Empty;
        }

        return spriteId.Trim().ToLowerInvariant();
    }
}
