public sealed class SaveRunSnapshotPurger
{
    public PersistResult Purge(ISaveSlotStore repository, int proceduralSeed)
    {
        repository.EnsureSlotsExist();
        var purged = 0;
        var failed = 0;
        foreach (var slotId in SaveServiceSettings.SlotIds)
        {
            var payload = repository.ReadSanitized(slotId);
            var runState = SaveRuntimeSnapshotMapper.ResolveRunState(payload.Runtime);
            if (!payload.Meta.IsUsed || !runState.IsRunActive)
            {
                continue;
            }

            if (proceduralSeed > 0 && runState.ProceduralSeed != proceduralSeed)
            {
                continue;
            }

            payload.Runtime = ClearRunState(payload.Runtime);
            if (repository.WriteSlot(slotId, payload))
            {
                purged++;
                continue;
            }

            failed++;
        }

        if (failed > 0)
        {
            var changed = purged > 0;
            var message = changed
                ? "Pulizia snapshot run parziale: alcuni slot non sono stati aggiornati."
                : "Pulizia snapshot run fallita: nessuno slot aggiornato.";
            return PersistResult.Failed(SceneRoute.None, message, recoverable: true, persistenceChanged: changed);
        }

        var successText = purged > 0 ? "Snapshot run ripuliti." : "Nessuno snapshot run da ripulire.";
        return PersistResult.Succeeded(SceneRoute.None, persistenceChanged: purged > 0, message: successText);
    }

    private static SavedRuntime ClearRunState(SavedRuntime source)
    {
        var cleared = new SavedRuntime
        {
            World = new SavedWorld(),
            OverworldEnemies = new(),
            LastEncounterContext = source.LastEncounterContext,
            DeepestFloor = source.DeepestFloor,
            LabyrinthCompletions = source.LabyrinthCompletions,
            Run = new SavedRunState(),
        };
        SaveRuntimeSnapshotMapper.ApplyRunState(cleared, RunStatePolicy.ReturnToIndice(source.Run?.MaxFloors ?? source.ProceduralMaxFloors));
        return cleared;
    }
}
