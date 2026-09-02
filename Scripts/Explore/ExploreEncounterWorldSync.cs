using System.Collections.Generic;
using Godot;

public static class ExploreEncounterWorldSync
{
    public static void PruneConsumedAgents(List<EnemyAgent> enemyAgents, IReadOnlyList<OverworldEnemyModel> liveEnemies)
    {
        var liveSet = new HashSet<OverworldEnemyModel>(liveEnemies);
        for (var i = enemyAgents.Count - 1; i >= 0; i--)
        {
            var agent = enemyAgents[i];
            if (liveSet.Contains(agent.Model))
            {
                continue;
            }

            agent.QueueFree();
            enemyAgents.RemoveAt(i);
        }
    }

    public static void RefreshEnemyRuntime(
        OverworldEnemyRuntimeCoordinator? enemyRuntime,
        PlayerController player,
        DungeonData? dungeon,
        IReadOnlyList<OverworldEnemyModel> enemies)
    {
        if (enemyRuntime is null || dungeon is null)
        {
            return;
        }

        enemyRuntime.Tick(0f, player.GlobalPosition, dungeon, enemies);
    }
}
