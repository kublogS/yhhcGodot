using System;

public readonly struct EnemyVisualAnimationProfile
{
    public EnemyVisualAnimationProfile(
        float idleBobHeight,
        float idleBobSpeed,
        float moveBobHeight,
        float moveBobSpeed,
        float squashAmount,
        float pitchDegrees,
        float rollDegrees,
        float yawSwayDegrees,
        float lateralSway,
        float moveThreshold,
        float blendSpeed,
        bool symmetricMoveBob)
    {
        IdleBobHeight = idleBobHeight;
        IdleBobSpeed = idleBobSpeed;
        MoveBobHeight = moveBobHeight;
        MoveBobSpeed = moveBobSpeed;
        SquashAmount = squashAmount;
        PitchDegrees = pitchDegrees;
        RollDegrees = rollDegrees;
        YawSwayDegrees = yawSwayDegrees;
        LateralSway = lateralSway;
        MoveThreshold = moveThreshold;
        BlendSpeed = blendSpeed;
        SymmetricMoveBob = symmetricMoveBob;
    }

    public float IdleBobHeight { get; }
    public float IdleBobSpeed { get; }
    public float MoveBobHeight { get; }
    public float MoveBobSpeed { get; }
    public float SquashAmount { get; }
    public float PitchDegrees { get; }
    public float RollDegrees { get; }
    public float YawSwayDegrees { get; }
    public float LateralSway { get; }
    public float MoveThreshold { get; }
    public float BlendSpeed { get; }
    public bool SymmetricMoveBob { get; }
}

public static class EnemyVisualAnimationProfiles
{
    public static EnemyVisualAnimationProfile Resolve(string? spriteId, string? enemyName = null, string? specName = null)
    {
        var key = Normalize(spriteId, specName, enemyName);
        if (key.Contains("slime", StringComparison.OrdinalIgnoreCase))
        {
            return new EnemyVisualAnimationProfile(0.06f, 2.4f, 0.11f, 4.9f, 0.16f, 6f, 2f, 1f, 0.01f, 0.08f, 7.5f, false);
        }

        if (key.Contains("scheletro", StringComparison.OrdinalIgnoreCase) || key.Contains("skeleton", StringComparison.OrdinalIgnoreCase))
        {
            return new EnemyVisualAnimationProfile(0.025f, 1.8f, 0.05f, 4.3f, 0.035f, 4f, 7f, 4f, 0.01f, 0.07f, 8f, false);
        }

        if (key.Contains("bat", StringComparison.OrdinalIgnoreCase) || key.Contains("goblin", StringComparison.OrdinalIgnoreCase))
        {
            return new EnemyVisualAnimationProfile(0.08f, 4.8f, 0.12f, 7.8f, 0.05f, 14f, 18f, 6f, 0.05f, 0.06f, 10f, true);
        }

        if (key.Contains("dragon", StringComparison.OrdinalIgnoreCase) || key.Contains("orco", StringComparison.OrdinalIgnoreCase))
        {
            return new EnemyVisualAnimationProfile(0.035f, 1.25f, 0.085f, 2.9f, 0.06f, 9f, 5f, 2f, 0.015f, 0.07f, 5.5f, false);
        }

        return new EnemyVisualAnimationProfile(0.03f, 2f, 0.05f, 4.2f, 0.04f, 4f, 4f, 2f, 0.01f, 0.08f, 7f, false);
    }

    private static string Normalize(string? spriteId, string? specName, string? enemyName)
    {
        var primary = string.IsNullOrWhiteSpace(spriteId) ? string.Empty : spriteId.Trim().ToLowerInvariant();
        if (primary.Length > 0)
        {
            return primary;
        }

        var secondary = string.IsNullOrWhiteSpace(specName) ? string.Empty : specName.Trim().ToLowerInvariant();
        if (secondary.Length > 0)
        {
            return secondary;
        }

        return string.IsNullOrWhiteSpace(enemyName) ? string.Empty : enemyName.Trim().ToLowerInvariant();
    }
}
