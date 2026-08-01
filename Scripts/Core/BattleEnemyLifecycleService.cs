using System;
using System.Linq;

internal static class BattleEnemyLifecycleService
{
    public static void HandleDeadEnemies(GameState state, CombatTurnOutcome outcome)
    {
        var dead = state.Enemies.Where(enemy => !enemy.IsAlive).ToList();
        if (dead.Count == 0)
        {
            return;
        }

        foreach (var enemy in dead)
        {
            var deadSlot = state.Enemies.IndexOf(enemy);
            CombatService.PushEvent(outcome, CombatEventType.EnemyKnockedOut, $"{enemy.Name} è stato sconfitto.", sourceId: CombatService.PlayerActorId, targetId: CombatService.EnemyActorId(enemy, deadSlot), targetSlot: deadSlot);
        }

        state.Enemies = state.Enemies.Where(enemy => enemy.IsAlive).ToList();
        Inventory.GrantBattleLoot(state, dead);
        var gainedExp = dead.Sum(enemy => Math.Max(0, enemy.Exp));
        if (gainedExp > 0)
        {
            state.Player.Exp += gainedExp;
            CombatService.PushEvent(outcome, CombatEventType.SystemMessage, $"EXP +{gainedExp}", sourceId: CombatService.PlayerActorId);
        }

        outcome.DeadEnemies.AddRange(dead);
        while (state.BattleHasCapacity() && state.EnemyQueue.Count > 0)
        {
            var next = state.EnemyQueue[0];
            state.EnemyQueue.RemoveAt(0);
            state.Enemies.Add(next);
            outcome.EnteredEnemies.Add(next);
            var slot = state.Enemies.IndexOf(next);
            CombatService.PushEvent(outcome, CombatEventType.EnemyPromotedFromQueue, $"Un nemico entra in campo: {next.Name}.", sourceId: CombatService.EnemyActorId(next, slot), sourceSlot: slot);
        }
    }
}
