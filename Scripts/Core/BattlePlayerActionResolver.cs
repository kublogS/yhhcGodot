using System;
using System.Collections.Generic;
using System.Linq;

internal static class BattlePlayerActionResolver
{
    public static bool Resolve(GameState state, CombatTurnRequest request, CombatTurnOutcome outcome)
    {
        return request.ActionType switch
        {
            CombatActionType.Defend => ResolveDefend(outcome),
            CombatActionType.Items => ResolveItems(state.Player, request, outcome),
            CombatActionType.Flee => ResolveFlee(state, outcome),
            _ => ResolveAttack(state, request, outcome),
        };
    }

    private static bool ResolveDefend(CombatTurnOutcome outcome)
    {
        CombatService.PushEvent(outcome, CombatEventType.ActionUsed, "Hai usato Difenditi.", sourceId: CombatService.PlayerActorId);
        CombatService.PushEvent(outcome, CombatEventType.DefendActivated, "Ti difendi: difesa +75% per questa fase nemica.", sourceId: CombatService.PlayerActorId);
        return false;
    }

    private static bool ResolveItems(CharacterModel player, CombatTurnRequest request, CombatTurnOutcome outcome)
    {
        var itemId = BattleItemSelectionService.ResolveConsumable(player, request.SelectedItemId);
        if (string.IsNullOrWhiteSpace(itemId))
        {
            CombatService.PushEvent(outcome, CombatEventType.ItemUsed, "Hai provato a usare un oggetto.", sourceId: CombatService.PlayerActorId);
            CombatService.PushEvent(outcome, CombatEventType.NonDamagingAction, "Nessun oggetto disponibile.", sourceId: CombatService.PlayerActorId);
            return false;
        }

        CombatService.PushEvent(outcome, CombatEventType.ItemUsed, $"Hai usato {itemId}.", sourceId: CombatService.PlayerActorId);
        var healed = Inventory.UseConsumable(player, itemId);
        if (healed > 0)
        {
            CombatService.PushEvent(outcome, CombatEventType.Healed, $"Recuperi +{healed} HP.", sourceId: CombatService.PlayerActorId, targetId: CombatService.PlayerActorId, amount: healed);
            return false;
        }

        CombatService.PushEvent(outcome, CombatEventType.NonDamagingAction, "Nessun oggetto disponibile.", sourceId: CombatService.PlayerActorId);
        return false;
    }

    private static bool ResolveFlee(GameState state, CombatTurnOutcome outcome)
    {
        CombatService.PushEvent(outcome, CombatEventType.ActionUsed, "Hai tentato la fuga.", sourceId: CombatService.PlayerActorId);
        if (!CombatService.TryFlee(state))
        {
            CombatService.PushEvent(outcome, CombatEventType.FleeFailed, "Fuga fallita!", sourceId: CombatService.PlayerActorId);
            return false;
        }

        CombatService.PushEvent(outcome, CombatEventType.FleeSucceeded, "Fuga riuscita.", sourceId: CombatService.PlayerActorId);
        outcome.Fled = true;
        outcome.BattleEnded = true;
        return true;
    }

    private static bool ResolveAttack(GameState state, CombatTurnRequest request, CombatTurnOutcome outcome)
    {
        var player = state.Player;
        var move = request.SelectedMoveIndex >= 0 && request.SelectedMoveIndex < player.Moves.Count ? player.Moves[request.SelectedMoveIndex] : null;
        move ??= Moves.BasicAttackMove();
        if (!CombatService.CanCastMove(player, move))
        {
            CombatService.PushEvent(outcome, CombatEventType.SystemMessage, "Risorse insufficienti.", sourceId: CombatService.PlayerActorId);
            return false;
        }

        CombatService.ApplyMoveCost(player, move);
        var targets = state.Enemies.Where(enemy => enemy.IsAlive).ToList();
        if (targets.Count == 0)
        {
            return false;
        }

        if (!move.Aoe)
        {
            var targetIndex = Math.Clamp(request.SelectedTargetIndex, 0, targets.Count - 1);
            targets = new List<CharacterModel> { targets[targetIndex] };
        }

        CombatService.PushEvent(outcome, CombatEventType.ActionUsed, $"Hai usato {move.Name}.", sourceId: CombatService.PlayerActorId);
        foreach (var target in targets)
        {
            var targetSlot = state.Enemies.IndexOf(target);
            var targetId = CombatService.EnemyActorId(target, targetSlot);
            var (damage, tags) = CombatService.ComputeMoveDamage(player, target, move, state.Rng);
            var dealt = target.TakeDamage(damage);
            var typeTag = tags.AttackType is null ? string.Empty : $" ({tags.AttackType})";
            if (dealt > 0)
            {
                CombatService.PushEvent(outcome, CombatEventType.DamageDealt, $"{target.Name} subisce {dealt}{typeTag}{CombatService.FormatTags(tags)}.", sourceId: CombatService.PlayerActorId, targetId: targetId, targetSlot: targetSlot, amount: dealt, metadata: CombatService.BuildTagMetadata(tags));
                continue;
            }

            CombatService.PushEvent(outcome, CombatEventType.NonDamagingAction, $"{move.Name}{typeTag} non infligge danno a {target.Name}.", sourceId: CombatService.PlayerActorId, targetId: targetId, targetSlot: targetSlot);
        }

        return false;
    }
}
