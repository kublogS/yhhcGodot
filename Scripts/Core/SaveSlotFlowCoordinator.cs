using System.Collections.Generic;

public sealed class SaveSlotFlowCoordinator
{
    private readonly GameSession _session;
    private readonly SaveService _saveService;

    public SaveSlotFlowCoordinator(GameSession session, SaveService saveService)
    {
        _session = session;
        _saveService = saveService;
    }

    public List<SaveSlotView> ListSlots()
    {
        return _saveService.ListSlots();
    }

    public SceneRoute SelectSlotByIndex(int index)
    {
        var slotId = index + 1;
        _session.CurrentSlot = slotId;
        if (_saveService.LoadFromSlot(slotId))
        {
            return SceneRoute.Explore;
        }

        return SceneRoute.NameEntry;
    }

    public bool DeleteSlotByIndex(int index)
    {
        var slotId = IndexToSlotId(index);
        return slotId > 0 && _saveService.DeleteSlot(slotId);
    }

    public SaveCopyResult CopySlotByIndex(int sourceIndex, int targetIndex, bool overwriteExisting)
    {
        var sourceSlotId = IndexToSlotId(sourceIndex);
        var targetSlotId = IndexToSlotId(targetIndex);
        if (sourceSlotId <= 0 || targetSlotId <= 0)
        {
            return SaveCopyResult.InvalidSlot;
        }

        return _saveService.CopySlot(sourceSlotId, targetSlotId, overwriteExisting);
    }

    private static int IndexToSlotId(int index)
    {
        return index is >= 0 and <= 2 ? index + 1 : -1;
    }
}
