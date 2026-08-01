using System.Collections.Generic;
using Godot;

public sealed class ExploreTransitionFlow
{
    public float SpawnPortalEntryMinZ { get; set; }
    public float SpawnPortalEntryHalfWidth { get; set; } = 1.8f;
    public bool SpawnPortalTransitionLock { get; set; }
    public bool ExitTransitionLock { get; set; }

    public void HandleSpawnPortalCrossing(
        bool inSpawnHub,
        PlayerController player,
        PauseOverlayController pause,
        ManualOverlayController manual,
        MapOverlayController map,
        GameSession session,
        System.Action onEnterOverworld)
    {
        if (!inSpawnHub || ExploreModalFlow.IsModalActive(pause, manual, map) || SpawnPortalTransitionLock)
        {
            return;
        }

        var pos = player.GlobalPosition;
        if (Mathf.Abs(pos.X) > SpawnPortalEntryHalfWidth || pos.Z < SpawnPortalEntryMinZ)
        {
            return;
        }

        SpawnPortalTransitionLock = true;
        session.BeginOverworldRun();
        onEnterOverworld();
    }

    public bool TryHandleExitTransition(
        GameSession session,
        PlayerController player,
        List<EnemyAgent> enemyAgents,
        System.Action onAdvanceFloor,
        System.Action onReturnToHub)
    {
        var dungeon = session.CurrentDungeon;
        if (dungeon is null)
        {
            return false;
        }

        var tile = DungeonGenerator.WorldToGrid(player.GlobalPosition, DungeonMetrics.TileSize);
        if (dungeon.GetTile(tile.X, tile.Y) != TileType.Exit)
        {
            ExitTransitionLock = false;
            return false;
        }

        if (ExitTransitionLock)
        {
            return false;
        }

        if (HasAliveFloorBoss(enemyAgents))
        {
            GD.Print("[Dungeon] Uscita bloccata: boss del piano ancora attivo.");
            return false;
        }

        ExitTransitionLock = true;
        if (session.TryAdvanceOverworldFloor())
        {
            onAdvanceFloor();
            return true;
        }

        onReturnToHub();
        return true;
    }

    private static bool HasAliveFloorBoss(IEnumerable<EnemyAgent> enemyAgents)
    {
        foreach (var agent in enemyAgents)
        {
            if (agent.Model.Active && agent.Model.IsFloorBoss)
            {
                return true;
            }
        }

        return false;
    }
}
