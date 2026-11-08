using System;
using System.Collections.Generic;

// What the dungeon's enemies have puzzled out about the player's type this
// battle, from watching their own attacks land - never the ground truth.
// Reset at the start of every encounter (see GameState.ResetBattleInstance),
// so each fight is a fresh guessing game rather than carried-over knowledge.
public sealed class EnemyPlayerIntel
{
    private const float WeaknessSignal = 1f;
    private const float FamilySignal = 0.5f;
    private const float NeutralPenalty = 0.3f;
    private const float ConfidenceThreshold = 1.5f;

    private readonly Dictionary<string, float> _belief = new(StringComparer.OrdinalIgnoreCase);
    private string? _confirmedType;

    public void RecordObservation(DamageTags tags)
    {
        if (tags.AttackType is null || _confirmedType is not null)
        {
            return;
        }

        if (tags.Nullified)
        {
            // Same-type-as-defender is the nullify rule itself: this is a
            // direct reveal, not an inference.
            _confirmedType = tags.AttackType;
            return;
        }

        var cfg = TypeSystem.GetConfig();
        if (tags.Weak)
        {
            foreach (var candidate in cfg.Types)
            {
                if (TypeSystem.IsWeak(candidate, tags.AttackType))
                {
                    Adjust(candidate, WeaknessSignal);
                }
            }

            return;
        }

        if (tags.SameFamily || tags.EnemyFamily)
        {
            var attackFamily = TypeSystem.FamilyOfType(tags.AttackType);
            foreach (var candidate in cfg.Types)
            {
                var candidateFamily = TypeSystem.FamilyOfType(candidate);
                if (attackFamily is null || candidateFamily is null)
                {
                    continue;
                }

                var matches = tags.SameFamily
                    ? candidateFamily == attackFamily
                    : TypeSystem.FamiliesAreEnemy(attackFamily, candidateFamily);
                if (matches)
                {
                    Adjust(candidate, FamilySignal);
                }
            }

            return;
        }

        // A plain, unmodified hit rules candidates OUT: the true type is not
        // one that would have triggered weakness/nullify/family bonuses
        // against this attack type, so nudge those candidates down.
        foreach (var candidate in cfg.Types)
        {
            if (WouldTriggerAnySignal(candidate, tags.AttackType))
            {
                Adjust(candidate, -NeutralPenalty);
            }
        }
    }

    public string? GuessDefenderType()
    {
        if (_confirmedType is not null)
        {
            return _confirmedType;
        }

        string? best = null;
        var bestScore = ConfidenceThreshold;
        foreach (var pair in _belief)
        {
            if (pair.Value >= bestScore)
            {
                bestScore = pair.Value;
                best = pair.Key;
            }
        }

        return best;
    }

    private static bool WouldTriggerAnySignal(string candidateDefenderType, string attackType)
    {
        if (TypeSystem.IsWeak(candidateDefenderType, attackType))
        {
            return true;
        }

        if (candidateDefenderType.Equals(attackType, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var attackFamily = TypeSystem.FamilyOfType(attackType);
        var candidateFamily = TypeSystem.FamilyOfType(candidateDefenderType);
        if (attackFamily is null || candidateFamily is null)
        {
            return false;
        }

        return candidateFamily == attackFamily || TypeSystem.FamiliesAreEnemy(attackFamily, candidateFamily);
    }

    private void Adjust(string type, float delta)
    {
        _belief.TryGetValue(type, out var current);
        _belief[type] = current + delta;
    }
}
