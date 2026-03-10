using System.Text.Json;

public partial class SaveService
{
    public int PurgeRunSnapshots(int proceduralSeed)
    {
        EnsureSlotsExist();
        var purged = 0;
        foreach (var slotId in SlotIds)
        {
            var payload = SanitizePayload(ReadSlot(slotId), slotId);
            if (!payload.Meta.IsUsed || !payload.Runtime.RunActive)
            {
                continue;
            }

            if (proceduralSeed > 0 && payload.Runtime.ProceduralSeed != proceduralSeed)
            {
                continue;
            }

            payload.Runtime = ClearRunState(payload.Runtime);
            AtomicWrite(SlotPath(slotId), JsonSerializer.Serialize(payload, JsonOptions));
            purged++;
        }

        return purged;
    }

    private static SavedRuntime ClearRunState(SavedRuntime source)
    {
        return new SavedRuntime
        {
            World = new SavedWorld(),
            OverworldEnemies = new(),
            LastEncounterContext = source.LastEncounterContext,
            DeepestFloor = source.DeepestFloor,
            LabyrinthCompletions = source.LabyrinthCompletions,
            HasEnteredOverworld = false,
            RunActive = false,
            ProceduralSeed = 0,
            ProceduralFloor = 0,
            ProceduralMaxFloors = source.ProceduralMaxFloors <= 0 ? 20 : source.ProceduralMaxFloors,
        };
    }
}
