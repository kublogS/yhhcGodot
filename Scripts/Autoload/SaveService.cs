using System;
using System.Collections.Generic;
using Godot;

public partial class SaveService : Node, ISaveFlowPersistence
{
    public static SaveService Instance { get; private set; } = null!;
    private GameSession _session = null!;
    private readonly SaveSlotRepository _repository = new();
    private readonly SavePayloadFactory _payloadFactory = new();
    private readonly SaveRunSnapshotPurger _runSnapshotPurger = new();

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        _session = GetNode<GameSession>("/root/GameSession");
        EnsureSlotsExist();
    }

    public void EnsureSlotsExist()
    {
        _repository.EnsureSlotsExist();
    }

    public bool SaveToSlot(int slotId)
    {
        if (!_repository.IsValidSlot(slotId) || _session.State is null)
        {
            return false;
        }

        EnsureSlotsExist();
        var payload = _payloadFactory.BuildSessionPayload(_session, slotId);
        return _repository.WriteSlot(slotId, payload);
    }

    public bool LoadFromSlot(int slotId)
    {
        if (!IsValidSlot(slotId))
        {
            return false;
        }

        EnsureSlotsExist();
        var payload = _repository.ReadSanitized(slotId);
        if (!payload.Meta.IsUsed || payload.GameState is null)
        {
            return false;
        }

        SaveRuntimeSnapshotMapper.ApplyLoadedPayload(_session, payload, slotId);
        return true;
    }

    public List<SaveSlotView> ListSlots()
    {
        EnsureSlotsExist();
        var output = new List<SaveSlotView>();
        foreach (var slotId in SaveServiceSettings.SlotIds)
        {
            var payload = _repository.ReadSanitized(slotId);
            output.Add(new SaveSlotView
            {
                SlotId = slotId,
                IsUsed = payload.Meta.IsUsed,
                CharacterName = payload.HudPreview.CharacterName,
                DeepestFloor = payload.HudPreview.DeepestFloor,
                LabyrinthCompletions = payload.HudPreview.LabyrinthCompletions,
                LastSaveText = SavePayloadFactory.FormatLastSave(payload.Meta.LastSaveTs),
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
        var payload = _repository.ReadSanitized(slotId);
        if (!payload.Meta.IsUsed)
        {
            return false;
        }

        return _repository.WriteSlot(slotId, SaveSlotRepository.DefaultSlot(slotId));
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
        var source = _repository.ReadSanitized(sourceSlotId);
        if (!source.Meta.IsUsed || source.GameState is null)
        {
            return SaveCopyResult.SourceEmpty;
        }

        var target = _repository.ReadSanitized(targetSlotId);
        if (target.Meta.IsUsed && !overwriteExisting)
        {
            return SaveCopyResult.TargetOccupied;
        }

        var clone = _repository.CloneToSlot(source, targetSlotId);
        clone.Meta.IsUsed = true;
        clone.Meta.SlotId = targetSlotId;
        clone.Meta.SaveVersion = SaveServiceSettings.SaveVersion;
        clone.Meta.LastSaveTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return _repository.WriteSlot(targetSlotId, clone)
            ? SaveCopyResult.Success
            : SaveCopyResult.WriteFailed;
    }

    public PersistResult PurgeRunSnapshots(int proceduralSeed)
    {
        return _runSnapshotPurger.Purge(_repository, proceduralSeed);
    }

    private bool IsValidSlot(int slotId) => _repository.IsValidSlot(slotId);
}
