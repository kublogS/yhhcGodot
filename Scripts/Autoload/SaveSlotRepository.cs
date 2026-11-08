using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using FileAccess = Godot.FileAccess;

public sealed class SaveSlotRepository : ISaveSlotStore
{
    private readonly string _slotsDirectory;

    public SaveSlotRepository(string slotsDirectory = "user://saves")
    {
        _slotsDirectory = slotsDirectory;
    }

    public void EnsureSlotsExist()
    {
        DirAccess.MakeDirRecursiveAbsolute(_slotsDirectory);
        foreach (var slotId in SaveServiceSettings.SlotIds)
        {
            var path = SlotPath(slotId);
            RecoverInterruptedWrite(path);
            if (!FileAccess.FileExists(path))
            {
                WriteSlot(slotId, DefaultSlot(slotId));
            }
        }
    }

    public bool IsValidSlot(int slotId)
    {
        return Array.Exists(SaveServiceSettings.SlotIds, id => id == slotId);
    }

    public SaveFileData ReadSanitized(int slotId)
    {
        return SanitizePayload(ReadSlot(slotId), slotId);
    }

    public bool WriteSlot(int slotId, SaveFileData payload)
    {
        var json = JsonSerializer.Serialize(payload, SaveServiceSettings.JsonOptions);
        return AtomicWrite(SlotPath(slotId), json);
    }

    public SaveFileData CloneToSlot(SaveFileData source, int slotId)
    {
        var json = JsonSerializer.Serialize(source, SaveServiceSettings.JsonOptions);
        var clone = JsonSerializer.Deserialize<SaveFileData>(json, SaveServiceSettings.JsonOptions);
        return SanitizePayload(clone, slotId);
    }

    public static SaveFileData DefaultSlot(int slotId)
    {
        return new SaveFileData
        {
            Meta = new SlotMeta
            {
                IsUsed = false,
                SlotId = slotId,
                SaveVersion = SaveServiceSettings.SaveVersion,
                LastSaveTs = 0,
                PlaytimeSeconds = 0,
            },
            HudPreview = new HudPreview(),
            Runtime = new SavedRuntime(),
            Rng = new SavedGameRng(),
        };
    }

    private SaveFileData ReadSlot(int slotId)
    {
        var path = SlotPath(slotId);
        RecoverInterruptedWrite(path);
        if (!FileAccess.FileExists(path))
        {
            return DefaultSlot(slotId);
        }

        var raw = ReadAllText(path);
        if (raw is null)
        {
            LogError($"Impossibile aprire il salvataggio {path}: {FileAccess.GetOpenError()}");
            return DefaultSlot(slotId);
        }

        if (!TryDeserialize(raw, out var parsed, out var error))
        {
            LogError($"Salvataggio corrotto o non leggibile in {path}: {error}");
            QuarantineCorrupted(path, raw);
            return DefaultSlot(slotId);
        }

        return SanitizePayload(parsed, slotId);
    }

    public static SaveFileData DeserializeOrDefault(string raw, int slotId, string sourceLabel = "slot")
    {
        if (TryDeserialize(raw, out var parsed, out var error))
        {
            return SanitizePayload(parsed, slotId);
        }

        LogError($"Salvataggio corrotto o non leggibile in {sourceLabel}: {error}");
        return DefaultSlot(slotId);
    }

    private static bool TryDeserialize(string raw, out SaveFileData? parsed, out string error)
    {
        try
        {
            parsed = JsonSerializer.Deserialize<SaveFileData>(raw, SaveServiceSettings.JsonOptions);
            error = parsed is null ? "payload vuoto (null)" : string.Empty;
            return parsed is not null;
        }
        catch (Exception ex)
        {
            parsed = null;
            error = ex.Message;
            return false;
        }
    }

    // AtomicWrite renames slot -> .bak and then .tmp -> slot. If the game dies
    // between those two renames there is no slot file on next launch, and
    // EnsureSlotsExist would write an empty slot whose AtomicWrite then deletes
    // the .bak: the save is gone. Promote the newest readable leftover instead
    // (.tmp is complete and flushed before the first rename, so it wins).
    private static void RecoverInterruptedWrite(string path)
    {
        if (FileAccess.FileExists(path))
        {
            return;
        }

        foreach (var candidate in new[] { path + ".tmp", path + ".bak" })
        {
            if (!FileAccess.FileExists(candidate))
            {
                continue;
            }

            var raw = ReadAllText(candidate);
            if (raw is null || !TryDeserialize(raw, out _, out _))
            {
                continue;
            }

            var err = DirAccess.RenameAbsolute(candidate, path);
            if (err == Error.Ok)
            {
                LogError($"Salvataggio interrotto: ripristinato {path} da {candidate}");
                return;
            }

            LogError($"Impossibile ripristinare {path} da {candidate}: {err}");
        }
    }

    // Keep a copy of an unreadable slot before the UI shows it as empty, so the
    // next save into that slot does not destroy the only evidence/data.
    private static void QuarantineCorrupted(string path, string raw)
    {
        var quarantinePath = path + ".corrupt";
        if (FileAccess.FileExists(quarantinePath))
        {
            return;
        }

        using var file = FileAccess.Open(quarantinePath, FileAccess.ModeFlags.Write);
        if (file is null)
        {
            LogError($"Impossibile mettere in quarantena il save corrotto {path}: {FileAccess.GetOpenError()}");
            return;
        }

        file.StoreString(raw);
    }

    private static string? ReadAllText(string path)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        return file?.GetAsText();
    }

    private static SaveFileData SanitizePayload(SaveFileData? payload, int slotId)
    {
        var safe = payload ?? DefaultSlot(slotId);
        safe.Meta ??= new SlotMeta();
        safe.HudPreview ??= new HudPreview();
        safe.Runtime ??= new SavedRuntime();
        safe.Runtime.World ??= new SavedWorld();
        safe.Runtime.World.BrokenBreakablePairs ??= new List<int>();
        safe.Runtime.OverworldEnemies ??= new List<OverworldEnemyModel>();
        safe.Runtime.Run ??= new SavedRunState();

        safe.Rng ??= new SavedGameRng();
        safe.Meta.SlotId = slotId;
        if (safe.Meta.SaveVersion <= 0)
        {
            safe.Meta.SaveVersion = SaveServiceSettings.SaveVersion;
        }

        return safe;
    }

    private string SlotPath(int slotId)
    {
        return $"{_slotsDirectory}/slot_{slotId}.json";
    }

    private static bool AtomicWrite(string path, string payload)
    {
        var tmpPath = path + ".tmp";
        var backupPath = path + ".bak";
        RemoveIfExists(tmpPath);
        RemoveIfExists(backupPath);
        using (var tmp = FileAccess.Open(tmpPath, FileAccess.ModeFlags.Write))
        {
            if (tmp is null)
            {
                LogError($"Impossibile aprire il file temporaneo di salvataggio {tmpPath}: {FileAccess.GetOpenError()}");
                return false;
            }

            tmp.StoreString(payload);
            tmp.Flush();
        }

        if (FileAccess.FileExists(path))
        {
            var backupErr = DirAccess.RenameAbsolute(path, backupPath);
            if (backupErr != Error.Ok)
            {
                LogError($"Impossibile creare il backup del save {path}: {backupErr}");
                RemoveIfExists(tmpPath);
                return false;
            }
        }

        var renameErr = DirAccess.RenameAbsolute(tmpPath, path);
        if (renameErr != Error.Ok)
        {
            LogError($"Impossibile completare il salvataggio atomico {tmpPath} -> {path}: {renameErr}");
            if (FileAccess.FileExists(backupPath))
            {
                var rollbackErr = DirAccess.RenameAbsolute(backupPath, path);
                if (rollbackErr != Error.Ok)
                {
                    LogError($"Impossibile ripristinare il backup del save {path}: {rollbackErr}");
                }
            }

            RemoveIfExists(tmpPath);
            return false;
        }

        if (FileAccess.FileExists(backupPath))
        {
            var cleanupErr = DirAccess.RemoveAbsolute(backupPath);
            if (cleanupErr != Error.Ok)
            {
                LogError($"Impossibile pulire il backup del save {backupPath}: {cleanupErr}");
            }
        }

        return true;
    }

    private static void RemoveIfExists(string path)
    {
        if (!FileAccess.FileExists(path))
        {
            return;
        }

        var removeErr = DirAccess.RemoveAbsolute(path);
        if (removeErr != Error.Ok)
        {
            LogError($"Impossibile rimuovere il file {path}: {removeErr}");
        }
    }

    private static void LogError(string message) => GameLog.Error(message);
}
