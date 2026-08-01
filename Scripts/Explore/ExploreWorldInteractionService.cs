using Godot;

public sealed class ExploreWorldInteractionService
{
    private const float ClickInteractDistance = 6f;

    public bool IsManualClicked(InputEvent @event, PlayerController player, bool isModalOpen)
    {
        if (isModalOpen)
        {
            return false;
        }

        if (@event is not InputEventMouseButton mouse || !mouse.Pressed || mouse.ButtonIndex != MouseButton.Left)
        {
            return false;
        }

        var hit = WorldInteractionRaycaster.Raycast(player.GetViewCamera(), ClickInteractDistance);
        return WorldInteractionRaycaster.HasGroupInHierarchy(hit, WorldInteractionGroups.ManualSheet);
    }

    public bool TryUseSaveSanctuary(
        GameSession session,
        ISaveFlowPersistence saveService,
        PlayerController player,
        HudOverlayController hud,
        bool inSpawnHub)
    {
        if (inSpawnHub || session.CurrentDungeon is null)
        {
            return false;
        }

        var tile = DungeonGenerator.WorldToGrid(player.GlobalPosition, DungeonMetrics.TileSize);
        if (session.CurrentDungeon.GetTile(tile.X, tile.Y) != TileType.Save)
        {
            return false;
        }

        var saved = saveService.SaveToSlot(session.CurrentSlot);
        hud.ShowTransientStatus(saved
            ? $"Salvato nello slot {session.CurrentSlot}"
            : "Salvataggio fallito");
        GD.Print(saved
            ? $"[Save] Slot {session.CurrentSlot} aggiornato nel santuario."
            : "[Save] Salvataggio fallito nel santuario.");
        return true;
    }

    public bool TryBreakFacingBreakable(GameSession session, PlayerController player, Node3D dungeonRoot)
    {
        var dungeon = session.CurrentDungeon;
        if (dungeon is null)
        {
            return false;
        }

        var forward = -player.GlobalTransform.Basis.Z;
        forward.Y = 0f;
        if (forward.LengthSquared() < 0.0001f)
        {
            return false;
        }

        forward = forward.Normalized();
        var probe = player.GlobalPosition + (forward * 0.9f);
        var tile = DungeonGenerator.WorldToGrid(probe, DungeonMetrics.TileSize);
        if (!dungeon.TryBreakBreakable(tile.X, tile.Y))
        {
            return false;
        }

        DungeonBuilder.TryRemoveBreakableNode(dungeonRoot, tile);
        GD.Print($"[Dungeon] Muro rompibile aperto a {tile}.");
        return true;
    }
}
