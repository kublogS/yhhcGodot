using System;
using System.Collections.Generic;
using Godot;

public readonly struct EnemyMotionDecision
{
    public EnemyMotionDecision(float vxPerSec, float vyPerSec, bool chasing)
    {
        VxPerSec = vxPerSec;
        VyPerSec = vyPerSec;
        Chasing = chasing;
    }

    public float VxPerSec { get; }
    public float VyPerSec { get; }
    public bool Chasing { get; }
    public static EnemyMotionDecision Idle => new(0f, 0f, false);
}

public sealed class OverworldEnemyRuntimeCoordinator
{
    public const float DecisionIntervalSec = 0.18f;
    public const float EncounterAlertDurationSec = 5f;

    private readonly Dictionary<string, EnemyMotionDecision> _cache = new(StringComparer.Ordinal);
    private readonly GameRng _rng;
    private float _decisionAccum;
    private float _alertTimer;

    public OverworldEnemyRuntimeCoordinator(GameRng rng)
    {
        _rng = rng;
    }

    public const float ParanoidAskCooldownSec = 3f;

    public float AlertTimer => _alertTimer;

    public void Tick(float dt, Vector3 playerWorldPos, DungeonData dungeon, IReadOnlyList<OverworldEnemyModel> enemies)
    {
        if (dt > 0f)
        {
            _alertTimer = MathF.Max(0f, _alertTimer - dt);
            _decisionAccum += dt;
        }

        var recompute = _decisionAccum >= DecisionIntervalSec || _cache.Count == 0;
        if (recompute)
        {
            _decisionAccum = 0f;
        }

        var liveIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var enemy in enemies)
        {
            if (!enemy.Active)
            {
                continue;
            }

            var enemyId = ResolveId(enemy);
            liveIds.Add(enemyId);
            OverworldAI.TickEnemyRuntime(enemy, dt);

            if (enemy.BehaviorKind == OverworldBehavior.Paranoico)
            {
                enemy.AskAroundCooldownSec = MathF.Max(0f, enemy.AskAroundCooldownSec - dt);
                if (enemy.AskAroundCooldownSec <= 0f)
                {
                    OverworldAI.ParanoidAskAround(enemy, enemies);
                    enemy.AskAroundCooldownSec = ParanoidAskCooldownSec;
                }
            }

            if (recompute || !_cache.ContainsKey(enemyId))
            {
                var (vxPerSec, vyPerSec, chasing) = OverworldAI.ComputeVelocity(
                    enemy,
                    playerWorldPos.X,
                    playerWorldPos.Z,
                    dungeon,
                    _alertTimer);
                _cache[enemyId] = new EnemyMotionDecision(vxPerSec, vyPerSec, chasing);
            }

            OverworldAI.MaybeWakeStuckAlly(enemy, enemies, _rng);
        }

        PruneStaleCache(liveIds);
    }

    public EnemyMotionDecision GetDecision(OverworldEnemyModel enemy)
    {
        var enemyId = ResolveId(enemy);
        return _cache.TryGetValue(enemyId, out var decision) ? decision : EnemyMotionDecision.Idle;
    }

    public bool ShouldStartEncounterOnContact(OverworldEnemyModel enemy)
    {
        var decision = GetDecision(enemy);
        return OverworldAI.ShouldStartEncounterOnContact(enemy, decision.Chasing, _alertTimer);
    }

    public void RaiseEncounterAlert(float durationSec = EncounterAlertDurationSec)
    {
        _alertTimer = MathF.Max(_alertTimer, durationSec);
    }

    private void PruneStaleCache(HashSet<string> liveIds)
    {
        if (_cache.Count == 0)
        {
            return;
        }

        var stale = new List<string>();
        foreach (var id in _cache.Keys)
        {
            if (!liveIds.Contains(id))
            {
                stale.Add(id);
            }
        }

        foreach (var id in stale)
        {
            _cache.Remove(id);
        }
    }

    private static string ResolveId(OverworldEnemyModel enemy)
    {
        if (!string.IsNullOrWhiteSpace(enemy.EnemyId))
        {
            return enemy.EnemyId.Trim();
        }

        return $"{enemy.Name}@{enemy.GetHashCode()}";
    }
}
