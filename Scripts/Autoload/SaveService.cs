using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using FileAccess = Godot.FileAccess;

public partial class SaveService : Node
{
    public static SaveService Instance { get; private set; } = null!;

    private const int SaveVersion = 1;
    private static readonly int[] SlotIds = { 1, 2, 3 };
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        EnsureSlotsExist();
    }

    public void EnsureSlotsExist()
    {
        DirAccess.MakeDirRecursiveAbsolute("user://saves");
        foreach (var slotId in SlotIds)
        {
            var path = SlotPath(slotId);
            if (!FileAccess.FileExists(path))
            {
                AtomicWrite(path, JsonSerializer.Serialize(DefaultSlot(slotId), JsonOptions));
                continue;
            }

            var payload = SanitizePayload(ReadSlot(slotId), slotId);
            AtomicWrite(path, JsonSerializer.Serialize(payload, JsonOptions));
        }
    }

    public bool SaveToSlot(int slotId)
    {
        if (!IsValidSlot(slotId) || GameSession.Instance.State is null)
        {
            return false;
        }

        EnsureSlotsExist();
        var session = GameSession.Instance;
        var payload = BuildSessionPayload(session, slotId);
        AtomicWrite(SlotPath(slotId), JsonSerializer.Serialize(payload, JsonOptions));
        return true;
    }

    public bool LoadFromSlot(int slotId)
    {
        if (!IsValidSlot(slotId))
        {
            return false;
        }

        EnsureSlotsExist();
        var payload = SanitizePayload(ReadSlot(slotId), slotId);
        if (!payload.Meta.IsUsed || payload.GameState is null)
        {
            return false;
        }

        ApplyLoadedPayload(GameSession.Instance, payload, slotId);
        return true;
    }

    public List<SaveSlotView> ListSlots()
    {
        EnsureSlotsExist();
        var output = new List<SaveSlotView>();
        foreach (var slotId in SlotIds)
        {
            var payload = SanitizePayload(ReadSlot(slotId), slotId);
            output.Add(new SaveSlotView
            {
                SlotId = slotId,
                IsUsed = payload.Meta.IsUsed,
                CharacterName = payload.HudPreview.CharacterName,
                DeepestFloor = payload.HudPreview.DeepestFloor,
                LabyrinthCompletions = payload.HudPreview.LabyrinthCompletions,
                LastSaveText = FormatLastSave(payload.Meta.LastSaveTs),
            });
        }

        return output;
    }

    public bool DeleteSlot(int slotId)
    {
        if (!IsValidSlot(slotId))
        {
            return false;
        }

        EnsureSlotsExist();
        var payload = SanitizePayload(ReadSlot(slotId), slotId);
        if (!payload.Meta.IsUsed)
        {
            return false;
        }

        AtomicWrite(SlotPath(slotId), JsonSerializer.Serialize(DefaultSlot(slotId), JsonOptions));
        return true;
    }

    public SaveCopyResult CopySlot(int sourceSlotId, int targetSlotId, bool overwriteExisting)
    {
        if (!IsValidSlot(sourceSlotId) || !IsValidSlot(targetSlotId))
        {
            return SaveCopyResult.InvalidSlot;
        }

        if (sourceSlotId == targetSlotId)
        {
            return SaveCopyResult.SameSlot;
        }

        EnsureSlotsExist();
        var source = SanitizePayload(ReadSlot(sourceSlotId), sourceSlotId);
        if (!source.Meta.IsUsed || source.GameState is null)
        {
            return SaveCopyResult.SourceEmpty;
        }

        var target = SanitizePayload(ReadSlot(targetSlotId), targetSlotId);
        if (target.Meta.IsUsed && !overwriteExisting)
        {
            return SaveCopyResult.TargetOccupied;
        }

        var clone = CloneToSlot(source, targetSlotId);
        clone.Meta.IsUsed = true;
        clone.Meta.SlotId = targetSlotId;
        clone.Meta.SaveVersion = SaveVersion;
        clone.Meta.LastSaveTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        AtomicWrite(SlotPath(targetSlotId), JsonSerializer.Serialize(clone, JsonOptions));
        return SaveCopyResult.Success;
    }

    private static SaveFileData BuildSessionPayload(GameSession session, int slotId)
    {
        var payload = DefaultSlot(slotId);
        payload.Meta.IsUsed = true;
        payload.Meta.SaveVersion = SaveVersion;
        payload.Meta.LastSaveTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        payload.Meta.PlaytimeSeconds = 0;
        payload.HudPreview.CharacterName = session.State!.Player.Name;
        payload.HudPreview.DeepestFloor = session.DeepestFloor;
        payload.HudPreview.LabyrinthCompletions = session.LabyrinthCompletions;
        payload.GameState = session.State;
        payload.Rng.RngState = session.State.Rng.State;
        payload.Runtime = BuildRuntimePayload(session);
        return payload;
    }

    private static string FormatLastSave(long timestamp)
    {
        if (timestamp <= 0)
        {
            return "-";
        }

        return DateTimeOffset.FromUnixTimeSeconds(timestamp)
            .ToLocalTime()
            .ToString("yyyy-MM-dd HH:mm");
    }

    private static SaveFileData CloneToSlot(SaveFileData source, int slotId)
    {
        var json = JsonSerializer.Serialize(source, JsonOptions);
        var clone = JsonSerializer.Deserialize<SaveFileData>(json, JsonOptions);
        return SanitizePayload(clone, slotId);
    }
}
