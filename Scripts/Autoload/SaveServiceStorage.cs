using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using FileAccess = Godot.FileAccess;

public partial class SaveService
{
    private SaveFileData ReadSlot(int slotId)
    {
        var path = SlotPath(slotId);
        if (!FileAccess.FileExists(path))
        {
            return DefaultSlot(slotId);
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        var raw = file.GetAsText();
        try
        {
            var parsed = JsonSerializer.Deserialize<SaveFileData>(raw, JsonOptions);
            return SanitizePayload(parsed, slotId);
        }
        catch
        {
            return DefaultSlot(slotId);
        }
    }

    private static SaveFileData SanitizePayload(SaveFileData? payload, int slotId)
    {
        var safe = payload ?? DefaultSlot(slotId);
        safe.Meta ??= new SlotMeta();
        safe.HudPreview ??= new HudPreview();
        safe.Runtime ??= new SavedRuntime();
        safe.Runtime.World ??= new SavedWorld();
        safe.Runtime.OverworldEnemies ??= new List<OverworldEnemyModel>();
        safe.Rng ??= new SavedGameRng();

        safe.Meta.SlotId = slotId;
        if (safe.Meta.SaveVersion <= 0)
        {
            safe.Meta.SaveVersion = SaveVersion;
        }

        return safe;
    }

    private static string SlotPath(int slotId)
    {
        return $"user://saves/slot_{slotId}.json";
    }

    private static SaveFileData DefaultSlot(int slotId)
    {
        return new SaveFileData
        {
            Meta = new SlotMeta
            {
                IsUsed = false,
                SlotId = slotId,
                SaveVersion = SaveVersion,
                LastSaveTs = 0,
                PlaytimeSeconds = 0,
            },
            HudPreview = new HudPreview(),
            Runtime = new SavedRuntime(),
            Rng = new SavedGameRng(),
        };
    }

    private static bool IsValidSlot(int slotId)
    {
        return Array.Exists(SlotIds, id => id == slotId);
    }

    private static void AtomicWrite(string path, string payload)
    {
        var tmpPath = path + ".tmp";
        using (var tmp = FileAccess.Open(tmpPath, FileAccess.ModeFlags.Write))
        {
            tmp.StoreString(payload);
        }

        if (FileAccess.FileExists(path))
        {
            DirAccess.RemoveAbsolute(path);
        }

        DirAccess.RenameAbsolute(tmpPath, path);
    }
}
