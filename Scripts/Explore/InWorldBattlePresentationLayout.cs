using System.Collections.Generic;
using System.Linq;
using Godot;

public static class InWorldBattlePresentationLayout
{
    public static void Apply(
        GameState state,
        Dictionary<string, InWorldBattleParticipant> participants,
        Vector3 battleCenter,
        Vector3 battleForward,
        Vector3 battleRight)
    {
        if (participants.TryGetValue(CombatService.PlayerActorId, out var playerParticipant))
        {
            var playerAnchor = battleCenter - (battleForward * 1.95f) - (battleRight * 1.35f);
            PlaceParticipant(playerParticipant, playerAnchor, battleCenter);
        }

        var count = Mathf.Min(4, state.Enemies.Count);
        for (var i = 0; i < count; i++)
        {
            var actorId = ResolveEnemyActorId(state.Enemies[i], i);
            if (!participants.TryGetValue(actorId, out var participant))
            {
                continue;
            }

            var spread = (i - ((count - 1) * 0.5f)) * 1.55f;
            var anchor = battleCenter + (battleForward * 1.65f) + (battleRight * spread) + new Vector3(0f, 0f, Mathf.Abs(spread) * 0.12f);
            participant.SetVisible(true);
            PlaceParticipant(participant, anchor, battleCenter - (battleForward * 1.2f));
        }

        var active = state.Enemies.Select((enemy, index) => ResolveEnemyActorId(enemy, index)).ToHashSet();
        foreach (var pair in participants)
        {
            if (pair.Key != CombatService.PlayerActorId && !active.Contains(pair.Key))
            {
                pair.Value.SetVisible(false);
            }
        }
    }

    public static List<EnemyAgent> ResolveInvolvedAgents(GameSession session, IReadOnlyList<EnemyAgent> enemyAgents)
    {
        var ids = session.EncounterWorldEnemies.Select(enemy => enemy.EnemyId).ToHashSet();
        return enemyAgents.Where(agent => ids.Contains(agent.Model.EnemyId)).ToList();
    }

    public static Vector3 ComputeBattleCenter(Vector3 playerPosition, IReadOnlyList<EnemyAgent> enemies)
    {
        var average = Vector3.Zero;
        foreach (var enemy in enemies)
        {
            average += enemy.GlobalPosition;
        }

        average /= Mathf.Max(1, enemies.Count);
        return (playerPosition + average) * 0.5f;
    }

    public static Vector3 ComputeBattleForward(Vector3 playerPosition, IReadOnlyList<EnemyAgent> enemies)
    {
        var direction = ComputeBattleCenter(playerPosition, enemies) - playerPosition;
        direction.Y = 0f;
        return direction.LengthSquared() < 0.001f ? Vector3.Forward : direction.Normalized();
    }

    public static string ResolveEnemyActorId(CharacterModel enemy, int slot)
    {
        return string.IsNullOrWhiteSpace(enemy.BattleActorId) ? $"enemy:{slot}:{enemy.Name}" : enemy.BattleActorId;
    }

    public static string? ResolveEnemySprite(GameState state, string actorId)
    {
        for (var i = 0; i < state.Enemies.Count; i++)
        {
            if (ResolveEnemyActorId(state.Enemies[i], i) == actorId)
            {
                return state.Enemies[i].Sprite;
            }
        }

        return null;
    }

    private static void PlaceParticipant(InWorldBattleParticipant participant, Vector3 anchor, Vector3 lookTarget)
    {
        var yaw = Mathf.Atan2((lookTarget - anchor).X, (lookTarget - anchor).Z);
        participant.SnapToAnchor(anchor, yaw);
    }
}
