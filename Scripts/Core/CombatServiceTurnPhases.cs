using System;
using System.Collections.Generic;
using System.Linq;

public static partial class CombatService
{
    private static void RunEnemyTurn(CharacterModel enemy, GameState state, CombatTurnOutcome outcome)
    {
        var player = state.Player;
        var enemySlot = state.Enemies.IndexOf(enemy);
        var sourceId = EnemyActorId(enemy, enemySlot);
        var enemyMove = PickCastableEnemyMove(enemy, state);
        if (enemyMove is null)
        {
            PushEvent(outcome, CombatEventType.ActionUsed, $"{enemy.Name} usa Attacco!", sourceId: sourceId, sourceSlot: enemySlot, targetId: PlayerActorId);
            var raw = RollAttack(enemy, player, state.Rng);
            var damage = player.Defending ? (int)MathF.Round(raw.RawDamage * 0.25f) : raw.RawDamage;
            EmitDefendMitigationEvent(outcome, raw.RawDamage, damage);
            var dealt = player.TakeDamage(damage);
            if (dealt > 0)
            {
                PushEvent(outcome, CombatEventType.DamageDealt, $"Danno ricevuto: {dealt} ({raw.Kind}).", sourceId: sourceId, targetId: PlayerActorId, sourceSlot: enemySlot, amount: dealt);
                return;
            }

            PushEvent(outcome, CombatEventType.NonDamagingAction, $"{enemy.Name} non infligge danno ({raw.Kind}).", sourceId: sourceId, sourceSlot: enemySlot, targetId: PlayerActorId);
            return;
        }

        ApplyMoveCost(enemy, enemyMove);
        PushEvent(outcome, CombatEventType.ActionUsed, $"{enemy.Name} usa {enemyMove.Name}.", sourceId: sourceId, sourceSlot: enemySlot, targetId: PlayerActorId);
        var (damageByMove, tags) = ComputeMoveDamage(enemy, player, enemyMove, state.Rng, player.Defending);
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
            PushEvent(outcome, CombatEventType.DamageDealt, $"{enemy.Name} infligge {dealtMove}{typeTag}{FormatTags(tags)}.", sourceId: sourceId, targetId: PlayerActorId, sourceSlot: enemySlot, amount: dealtMove);
            return;
        }

        PushEvent(outcome, CombatEventType.NonDamagingAction, $"{enemy.Name} non infligge danno con {enemyMove.Name}{typeTag}.", sourceId: sourceId, sourceSlot: enemySlot, targetId: PlayerActorId);
    }

    private static void HandleItems(CharacterModel player, CombatTurnRequest request, CombatTurnOutcome outcome)
    {
        var itemId = request.SelectedItemId;
        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemId = player.Inventory.Keys.FirstOrDefault(id => id.StartsWith("Cure", StringComparison.Ordinal));
        }

        if (string.IsNullOrWhiteSpace(itemId))
        {
            PushEvent(outcome, CombatEventType.ItemUsed, "Hai provato a usare un oggetto.", sourceId: PlayerActorId);
            PushEvent(outcome, CombatEventType.NonDamagingAction, "Nessun oggetto disponibile.", sourceId: PlayerActorId);
            return;
        }

        PushEvent(outcome, CombatEventType.ItemUsed, $"Hai usato {itemId}.", sourceId: PlayerActorId);
        var healed = Inventory.UseConsumable(player, itemId);
        if (healed > 0)
        {
            PushEvent(outcome, CombatEventType.Healed, $"Recuperi +{healed} HP.", sourceId: PlayerActorId, targetId: PlayerActorId, amount: healed);
            return;
        }

        PushEvent(outcome, CombatEventType.NonDamagingAction, "Nessun oggetto disponibile.", sourceId: PlayerActorId);
    }

    private static void HandlePlayerAttack(GameState state, CombatTurnRequest request, CombatTurnOutcome outcome)
    {
        var player = state.Player;
        var move = request.SelectedMoveIndex >= 0 && request.SelectedMoveIndex < player.Moves.Count ? player.Moves[request.SelectedMoveIndex] : null;
        move ??= Moves.BasicAttackMove();

        if (!CanCastMove(player, move))
        {
            PushEvent(outcome, CombatEventType.SystemMessage, "Risorse insufficienti.", sourceId: PlayerActorId);
            return;
        }

        ApplyMoveCost(player, move);
        var targets = state.Enemies.Where(e => e.IsAlive).ToList();
        if (targets.Count == 0)
        {
            return;
        }

        if (!move.Aoe)
        {
            var targetIndex = Math.Clamp(request.SelectedTargetIndex, 0, targets.Count - 1);
            targets = new List<CharacterModel> { targets[targetIndex] };
        }

        PushEvent(outcome, CombatEventType.ActionUsed, $"Hai usato {move.Name}.", sourceId: PlayerActorId);
        foreach (var target in targets)
        {
            var targetSlot = state.Enemies.IndexOf(target);
            var targetId = EnemyActorId(target, targetSlot);
            var (damage, tags) = ComputeMoveDamage(player, target, move, state.Rng);
            var dealt = target.TakeDamage(damage);
            var typeTag = tags.AttackType is null ? string.Empty : $" ({tags.AttackType})";
            if (dealt > 0)
            {
                PushEvent(outcome, CombatEventType.DamageDealt, $"{target.Name} subisce {dealt}{typeTag}{FormatTags(tags)}.", sourceId: PlayerActorId, targetId: targetId, targetSlot: targetSlot, amount: dealt);
                continue;
            }

            PushEvent(outcome, CombatEventType.NonDamagingAction, $"{move.Name}{typeTag} non infligge danno a {target.Name}.", sourceId: PlayerActorId, targetId: targetId, targetSlot: targetSlot);
        }
    }

    private static void HandleDeadEnemies(GameState state, CombatTurnOutcome outcome)
    {
        var dead = state.Enemies.Where(e => !e.IsAlive).ToList();
        if (dead.Count == 0)
        {
            return;
        }

        foreach (var enemy in dead)
        {
            var deadSlot = state.Enemies.IndexOf(enemy);
            PushEvent(outcome, CombatEventType.EnemyKnockedOut, $"{enemy.Name} è stato sconfitto.", sourceId: PlayerActorId, targetId: EnemyActorId(enemy, deadSlot), targetSlot: deadSlot);
        }

        state.Enemies = state.Enemies.Where(e => e.IsAlive).ToList();
        Inventory.GrantBattleLoot(state, dead);
        var gainedExp = dead.Sum(e => Math.Max(0, e.Exp));
        if (gainedExp > 0)
        {
            state.Player.Exp += gainedExp;
            PushEvent(outcome, CombatEventType.SystemMessage, $"EXP +{gainedExp}", sourceId: PlayerActorId);
        }

        outcome.DeadEnemies.AddRange(dead);
        while (state.BattleHasCapacity() && state.EnemyQueue.Count > 0)
        {
            var next = state.EnemyQueue[0];
            state.EnemyQueue.RemoveAt(0);
            state.Enemies.Add(next);
            outcome.EnteredEnemies.Add(next);
            var slot = state.Enemies.IndexOf(next);
            PushEvent(outcome, CombatEventType.EnemyPromotedFromQueue, $"Un nemico entra in campo: {next.Name}.", sourceId: EnemyActorId(next, slot), sourceSlot: slot);
        }

        state.SyncEnemyLegacy();
    }

    private static void EmitDefendMitigationEvent(CombatTurnOutcome outcome, int rawDamage, int reducedDamage)
    {
        if (rawDamage <= reducedDamage)
        {
            return;
        }

        PushEvent(outcome, CombatEventType.DamageMitigated, $"Difesa attiva: danno mitigato di {rawDamage - reducedDamage}.", sourceId: PlayerActorId, targetId: PlayerActorId, amount: rawDamage - reducedDamage);
    }
}
