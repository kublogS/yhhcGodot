using System;

internal static class BattleEnemyTurnResolver
{
    public static void Resolve(CharacterModel enemy, GameState state, CombatTurnOutcome outcome)
    {
        var player = state.Player;
        var enemySlot = state.Enemies.IndexOf(enemy);
        var sourceId = CombatService.EnemyActorId(enemy, enemySlot);
        var enemyMove = CombatService.PickCastableEnemyMove(enemy, state);
        if (enemyMove is null)
        {
            CombatService.PushEvent(outcome, CombatEventType.ActionUsed, $"{enemy.Name} usa Attacco!", sourceId: sourceId, sourceSlot: enemySlot, targetId: CombatService.PlayerActorId);
            var raw = CombatService.RollAttack(enemy, player, state.Rng);
            var damage = player.Defending ? (int)MathF.Round(raw.RawDamage * 0.25f) : raw.RawDamage;
            EmitDefendMitigationEvent(outcome, raw.RawDamage, damage);
            var dealt = player.TakeDamage(damage);
            if (dealt > 0)
            {
                CombatService.PushEvent(outcome, CombatEventType.DamageDealt, $"Danno ricevuto: {dealt} ({raw.Kind}).", sourceId: sourceId, targetId: CombatService.PlayerActorId, sourceSlot: enemySlot, amount: dealt);
                return;
            }

            CombatService.PushEvent(outcome, CombatEventType.NonDamagingAction, $"{enemy.Name} non infligge danno ({raw.Kind}).", sourceId: sourceId, sourceSlot: enemySlot, targetId: CombatService.PlayerActorId);
            return;
        }

        CombatService.ApplyMoveCost(enemy, enemyMove);
        CombatService.PushEvent(outcome, CombatEventType.ActionUsed, $"{enemy.Name} usa {enemyMove.Name}.", sourceId: sourceId, sourceSlot: enemySlot, targetId: CombatService.PlayerActorId);
        var (damageByMove, tags) = CombatService.ComputeMoveDamage(enemy, player, enemyMove, state.Rng, player.Defending);
        var preDefendDamage = damageByMove;
        if (player.Defending)
        {
            damageByMove = (int)MathF.Round(damageByMove * 0.25f);
            tags.Crit = false;
            tags.Weak = false;
        }

        EmitDefendMitigationEvent(outcome, preDefendDamage, damageByMove);
        var dealtMove = player.TakeDamage(damageByMove);
        var typeTag = tags.AttackType is null ? string.Empty : $" ({tags.AttackType})";
        if (dealtMove > 0)
        {
            CombatService.PushEvent(outcome, CombatEventType.DamageDealt, $"{enemy.Name} infligge {dealtMove}{typeTag}{CombatService.FormatTags(tags)}.", sourceId: sourceId, targetId: CombatService.PlayerActorId, sourceSlot: enemySlot, amount: dealtMove);
            return;
        }

        CombatService.PushEvent(outcome, CombatEventType.NonDamagingAction, $"{enemy.Name} non infligge danno con {enemyMove.Name}{typeTag}.", sourceId: sourceId, sourceSlot: enemySlot, targetId: CombatService.PlayerActorId);
    }

    private static void EmitDefendMitigationEvent(CombatTurnOutcome outcome, int rawDamage, int reducedDamage)
    {
        if (rawDamage <= reducedDamage)
        {
            return;
        }

        CombatService.PushEvent(outcome, CombatEventType.DamageMitigated, $"Difesa attiva: danno mitigato di {rawDamage - reducedDamage}.", sourceId: CombatService.PlayerActorId, targetId: CombatService.PlayerActorId, amount: rawDamage - reducedDamage);
    }
}
