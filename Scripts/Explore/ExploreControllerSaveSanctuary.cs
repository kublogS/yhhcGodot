using Godot;

public partial class ExploreController
{
    private bool TryUseSaveSanctuary()
    {
        var session = GameSession.Instance;
        var dungeon = session.CurrentDungeon;
        if (_inSpawnHub || dungeon is null)
        {
            return false;
        }

        var tile = DungeonGenerator.WorldToGrid(_player.GlobalPosition, DungeonBuilder.TileSize);
        if (dungeon.GetTile(tile.X, tile.Y) != TileType.Save)
        {
            return false;
        }

        var saved = SaveService.Instance.SaveToSlot(session.CurrentSlot);
        _hud.ShowTransientStatus(saved
            ? $"Salvato nello slot {session.CurrentSlot}"
            : "Salvataggio fallito");
        GD.Print(saved
            ? $"[Save] Slot {session.CurrentSlot} aggiornato nel santuario."
            : "[Save] Salvataggio fallito nel santuario.");
        return true;
    }
}
