using System;
using System.Collections.Generic;
using System.Linq;

public static partial class CombatService
{
    public static CombatTurnOutcome CombatTurn(GameState state, CombatTurnRequest request)
    {
        var outcome = new CombatTurnOutcome();
        var player = state.Player;
        if (state.Enemies.Count == 0)
        {
            outcome.LogLines.Add("Nessun nemico in campo.");
            return outcome;
        }

        player.Defending = request.ActionType == CombatActionType.Defend;
        if (request.ActionType == CombatActionType.Defend)
        {
            outcome.LogLines.Add("Ti difendi: difesa +75% per questo turno.");
        }
        else if (request.ActionType == CombatActionType.Items)
        {
            HandleItems(player, request, outcome);
        }
        else if (request.ActionType == CombatActionType.Flee)
        {
            if (TryFlee(state))
            {
                outcome.LogLines.Add("Fuga riuscita.");
                outcome.Fled = true;
                outcome.BattleEnded = true;
                return outcome;
            }

            outcome.LogLines.Add("Fuga fallita!");
        }
        else
        {
            HandlePlayerAttack(state, request, outcome);
        }

        HandleDeadEnemies(state, outcome);
        if (state.Enemies.Count == 0 && state.EnemyQueue.Count == 0)
        {
            outcome.BattleEnded = true;
            player.Defending = false;
            return outcome;
        }

        foreach (var enemy in state.Enemies.ToList())
        {
            if (!enemy.IsAlive || !player.IsAlive)
            {
                continue;
            }

            RunEnemyTurn(enemy, state, outcome);
            if (!player.IsAlive)
            {
                outcome.PlayerDefeated = true;
                outcome.LogLines.Add("Sei stato sconfitto...");
                break;
            }
        }

        player.Defending = false;
        HandleDeadEnemies(state, outcome);
        outcome.BattleEnded = state.Enemies.Count == 0 && state.EnemyQueue.Count == 0;
        return outcome;
    }

    private static void RunEnemyTurn(CharacterModel enemy, GameState state, CombatTurnOutcome outcome)
    {
        var player = state.Player;
        var enemyMove = PickCastableEnemyMove(enemy, state);
        if (enemyMove is null)
        {
            var raw = RollAttack(enemy, player, state.Rng);
            var dmg = player.Defending ? (int)MathF.Round(raw.RawDamage * 0.25f) : raw.RawDamage;
            var dealt = player.TakeDamage(dmg);
            outcome.LogLines.Add($"{enemy.Name} usa Attacco! Danno: {dealt} ({raw.Kind})");
            return;
        }

        ApplyMoveCost(enemy, enemyMove);
        var defendingNeutral = player.Defending;
        var (damage, tags) = ComputeMoveDamage(enemy, player, enemyMove, state.Rng, defendingNeutral);
        if (player.Defending)
        {
            damage = (int)MathF.Round(damage * 0.25f);
            tags.Crit = false;
            tags.Weak = false;
        }

        var dealtMove = player.TakeDamage(damage);
        var typeTag = tags.AttackType is null ? string.Empty : $" ({tags.AttackType})";
        outcome.LogLines.Add($"{enemy.Name} usa {enemyMove.Name}{typeTag}! Danno: {dealtMove}{FormatTags(tags)}");
    }

    private static void HandleItems(CharacterModel player, CombatTurnRequest request, CombatTurnOutcome outcome)
    {
        var itemId = request.SelectedItemId;
        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemId = player.Inventory.Keys.FirstOrDefault(id => id.StartsWith("Cure", StringComparison.Ordinal));
        }

        var healed = string.IsNullOrWhiteSpace(itemId) ? 0 : Inventory.UseConsumable(player, itemId);
        outcome.LogLines.Add(healed > 0 ? $"Hai usato {itemId}: +{healed} HP." : "Nessun oggetto disponibile.");
    }

    private static void HandlePlayerAttack(GameState state, CombatTurnRequest request, CombatTurnOutcome outcome)
    {
        var player = state.Player;
        var move = request.SelectedMoveIndex >= 0 && request.SelectedMoveIndex < player.Moves.Count ? player.Moves[request.SelectedMoveIndex] : null;
        move ??= Moves.BasicAttackMove();

        if (!CanCastMove(player, move))
        {
            outcome.LogLines.Add("Risorse insufficienti.");
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

        foreach (var target in targets)
        {
            var (damage, tags) = ComputeMoveDamage(player, target, move, state.Rng);
            var dealt = target.TakeDamage(damage);
            var typeTag = tags.AttackType is null ? string.Empty : $" ({tags.AttackType})";
            outcome.LogLines.Add($"Hai usato {move.Name}{typeTag} su {target.Name}! Danno: {dealt}{FormatTags(tags)}");
        }
    }

    private static void HandleDeadEnemies(GameState state, CombatTurnOutcome outcome)
    {
        var dead = state.Enemies.Where(e => !e.IsAlive).ToList();
        if (dead.Count == 0)
        {
            return;
        }

        state.Enemies = state.Enemies.Where(e => e.IsAlive).ToList();
        Inventory.GrantBattleLoot(state, dead);
        var gainedExp = dead.Sum(e => Math.Max(0, e.Exp));
        if (gainedExp > 0)
        {
            state.Player.Exp += gainedExp;
            outcome.LogLines.Add($"EXP +{gainedExp}");
        }

        outcome.DeadEnemies.AddRange(dead);
        while (state.BattleHasCapacity() && state.EnemyQueue.Count > 0)
        {
            var next = state.EnemyQueue[0];
            state.EnemyQueue.RemoveAt(0);
            state.Enemies.Add(next);
            outcome.EnteredEnemies.Add(next);
            outcome.LogLines.Add("Un nemico entra in campo!");
        }

        state.SyncEnemyLegacy();
    }
}
