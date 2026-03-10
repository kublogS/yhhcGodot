using System;
using System.Collections.Generic;

public static partial class CombatService
{
    public static AttackResult RollAttack(CharacterModel attacker, CharacterModel target, GameRng rng)
    {
        if (rng.NextInt(1, 100) <= Math.Max(0, target.Fortuna) * 5)
        {
            return new AttackResult { RawDamage = 0, Kind = "miss" };
        }

        var critChance = Math.Max(0, attacker.Fortuna) * 100f / 32f;
        if (rng.NextInt(1, 100) <= critChance)
        {
            return new AttackResult { RawDamage = Math.Max(0, attacker.Forza) * 2, Kind = "critico" };
        }

        return new AttackResult { RawDamage = Math.Max(0, attacker.Forza), Kind = "normale" };
    }

    public static int StealMoveChance(CharacterModel player, CharacterModel enemy)
    {
        var p = Math.Max(0, player.Agilita) + Math.Max(0, player.Fortuna);
        var e = Math.Max(0, enemy.Agilita) + Math.Max(0, enemy.Fortuna);
        return Math.Clamp(50 + (p - e) * 3, 10, 90);
    }

    public static (int Damage, DamageTags Tags) ComputeMoveDamage(CharacterModel attacker, CharacterModel defender, MoveModel move, GameRng rng, bool ignoreSpecialRules = false)
    {
        if (move.IsBasicAttack)
        {
            return (CombatBalanceConfig.BasicAttackBasePower, new DamageTags());
        }

        var cfg = TypeSystem.GetConfig();
        var tags = new DamageTags { AttackType = TypeSystem.MoveType(move) };
        var baseDamage = Math.Max(0, move.BaseDamage <= 0 ? 10 : move.BaseDamage);
        var statKey = tags.AttackType is null ? "attacco" : cfg.TypeStatScaling.GetValueOrDefault(tags.AttackType, "attacco");
        var offScale = cfg.Scaling.GetValueOrDefault("offense_per_stat_point", 0.01f);
        var defScale = cfg.Scaling.GetValueOrDefault("defense_per_stat_point", 0.01f);

        float damage = baseDamage;
        damage += damage * (offScale * OffenseStat(attacker, statKey));
        var defense = Math.Max(0, defender.Difesa);
        if (ignoreSpecialRules)
        {
            defense = (int)MathF.Round(defense * 1.75f);
        }

        var defenseFactor = MathF.Max(0f, 1f - (defScale * defense));
        damage *= defenseFactor;

        if (ignoreSpecialRules)
        {
            return (Math.Max(0, (int)MathF.Round(damage)), tags);
        }

        var mods = cfg.DamageModifiers;
        var defenderType = TypeSystem.PrimaryType(defender);
        var attackerType = TypeSystem.PrimaryType(attacker);
        var critEnabled = true;

        if (tags.AttackType is not null && defenderType is not null && TypeSystem.IsWeak(defenderType, tags.AttackType))
        {
            damage += damage * ModFloat(mods, "weakness_bonus", 0.5f);
            tags.Weak = true;
            if (ModBool(mods, "disable_crit_on_weakness", true))
            {
                critEnabled = false;
            }
        }
        else
        {
            if (ModBool(mods, "nullify_on_same_type_as_defender", true) && tags.AttackType is not null && defenderType is not null && tags.AttackType == defenderType)
            {
                tags.Nullified = true;
                return (0, tags);
            }

            var attackFam = TypeSystem.FamilyOfType(tags.AttackType);
            var defendFam = TypeSystem.FamilyOfType(defenderType);
            if (attackFam is not null && defendFam is not null)
            {
                if (attackFam == defendFam)
                {
                    damage += damage * ModFloat(mods, "same_family_penalty", -0.2f);
                    tags.SameFamily = true;
                }
                else if (TypeSystem.FamiliesAreEnemy(attackFam, defendFam))
                {
                    damage += damage * ModFloat(mods, "enemy_family_bonus", 0.2f);
                    tags.EnemyFamily = true;
                }
            }
        }

        if (tags.AttackType is not null && attackerType is not null && tags.AttackType == attackerType)
        {
            damage += damage * ModFloat(mods, "stab_bonus", 0.2f);
            tags.Stab = true;
        }

        if (critEnabled && RollCrit(attacker, rng))
        {
            damage += damage * ModFloat(mods, "crit_bonus", 0.5f);
            tags.Crit = true;
        }

        return (Math.Max(0, (int)MathF.Round(damage)), tags);
    }

    private static int OffenseStat(CharacterModel attacker, string statKey)
    {
        return statKey switch
        {
            "magia" => Math.Max(0, attacker.Magia),
            "fede" => Math.Max(0, attacker.Fede),
            "intelligence" => Math.Max(0, attacker.Intelligence == 0 ? attacker.Intelligenza : attacker.Intelligence),
            _ => Math.Max(0, attacker.Forza),
        };
    }

    private static bool RollCrit(CharacterModel attacker, GameRng rng)
    {
        return rng.NextInt(1, 100) <= Math.Max(0, attacker.Fortuna) * 100f / 32f;
    }

    private static float ModFloat(Dictionary<string, string> mods, string key, float fallback)
    {
        return mods.TryGetValue(key, out var val) && float.TryParse(val, out var num) ? num : fallback;
    }

    private static bool ModBool(Dictionary<string, string> mods, string key, bool fallback)
    {
        if (!mods.TryGetValue(key, out var val))
        {
            return fallback;
        }

        if (bool.TryParse(val, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }
}
