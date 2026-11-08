using System;

public sealed class SavePayloadFactory
{
    public SaveFileData BuildSessionPayload(GameSession session, int slotId)
    {
        var payload = SaveSlotRepository.DefaultSlot(slotId);
        payload.Meta.IsUsed = true;
        payload.Meta.SaveVersion = SaveServiceSettings.SaveVersion;
        payload.Meta.LastSaveTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        payload.Meta.PlaytimeSeconds = 0;
        payload.HudPreview.CharacterName = session.State!.Player.Name;
        payload.HudPreview.DeepestFloor = session.DeepestFloor;
        payload.HudPreview.LabyrinthCompletions = session.LabyrinthCompletions;
        payload.GameState = session.State;
        payload.Rng.RngState = session.State.Rng.State;
        payload.Runtime = SaveRuntimeSnapshotMapper.BuildRuntimePayload(session);
        return payload;
    }

    public static string FormatLastSave(long timestamp)
    {
        if (timestamp <= 0)
        {
            return "-";
        }

        return DateTimeOffset.FromUnixTimeSeconds(timestamp)
            .ToLocalTime()
            .ToString("yyyy-MM-dd HH:mm");
    }
}
