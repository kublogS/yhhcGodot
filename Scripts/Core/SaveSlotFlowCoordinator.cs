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

    public void DeleteSlotByIndex(int index)
    {
        _saveService.DeleteSlot(index + 1);
    }
}
