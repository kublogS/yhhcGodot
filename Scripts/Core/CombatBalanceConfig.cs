using System;

public static class CombatBalanceConfig
{
    private const float DefaultBaseFleeSuccessChance = 0.70f;
    private const int DefaultBasicAttackBasePower = 17;

    public static float BaseFleeSuccessChance => Clamp01(ReadScaling("base_flee_success_chance", DefaultBaseFleeSuccessChance));

    public static int BasicAttackBasePower
    {
        get
        {
            var raw = ReadScaling("basic_attack_base_power", DefaultBasicAttackBasePower);
            return Math.Max(1, (int)MathF.Round(raw));
        }
    }

    private static float ReadScaling(string key, float fallback)
    {
        var scaling = TypeSystem.GetConfig().Scaling;
        return scaling.TryGetValue(key, out var value) ? value : fallback;
    }

    private static float Clamp01(float value)
    {
        return value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
