using Godot;

public partial class ExploreController
{
    private bool _exitTransitionLock;

    private void HandleOverworldExitTransition()
    {
        var dungeon = GameSession.Instance.CurrentDungeon;
        if (dungeon is null)
        {
            return;
        }

        var tile = DungeonGenerator.WorldToGrid(_player.GlobalPosition, DungeonBuilder.TileSize);
        var onExit = dungeon.GetTile(tile.X, tile.Y) == TileType.Exit;
        if (!onExit)
        {
            _exitTransitionLock = false;
            return;
        }

        if (_exitTransitionLock)
        {
            return;
        }

        _exitTransitionLock = true;
        if (HasAliveFloorBoss())
        {
            GD.Print("[Dungeon] Uscita bloccata: boss del piano ancora attivo.");
            return;
        }

        if (GameSession.Instance.TryAdvanceOverworldFloor())
        {
            BuildSceneFromSession();
            return;
        }

        BuildSpawnHub();
    }

    private void TryBreakFacingBreakable()
    {
        var dungeon = GameSession.Instance.CurrentDungeon;
        if (dungeon is null)
        {
            return;
        }

        var forward = -_player.GlobalTransform.Basis.Z;
        forward.Y = 0f;
        if (forward.LengthSquared() < 0.0001f)
        {
            return;
        }

        forward = forward.Normalized();
        var probe = _player.GlobalPosition + (forward * 0.9f);
        var tile = DungeonGenerator.WorldToGrid(probe, DungeonBuilder.TileSize);
        if (dungeon.GetTile(tile.X, tile.Y) != TileType.Breakable)
        {
            return;
        }

        dungeon.Grid[tile.Y, tile.X] = (int)TileType.Floor;
        dungeon.BreakableTiles.Remove(tile);
        GD.Print($"[Dungeon] Muro rompibile aperto a {tile}.");
    }

    private bool HasAliveFloorBoss()
    {
        foreach (var agent in _enemyAgents)
        {
            if (agent.Model.Active && agent.Model.IsFloorBoss)
            {
                return true;
            }
        }

        return false;
    }
}
