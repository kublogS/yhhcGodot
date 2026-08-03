public interface ISaveSlotStore
{
    void EnsureSlotsExist();
    SaveFileData ReadSanitized(int slotId);
    bool WriteSlot(int slotId, SaveFileData payload);
}
