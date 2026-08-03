using Godot;

public static class ExploreModalFlow
{
    public static bool IsModalActive(
        PauseOverlayController pause,
        ManualOverlayController manual,
        MapOverlayController map)
    {
        return pause.Visible || manual.Visible || map.Visible;
    }

    public static void ApplyInputLock(
        PlayerController player,
        PauseOverlayController pause,
        ManualOverlayController manual,
        MapOverlayController map)
    {
        var interactive = !IsModalActive(pause, manual, map);
        player.SetLookEnabled(interactive);
        player.SetMovementEnabled(interactive);
    }

    public static void OpenManual(
        PlayerController player,
        PauseOverlayController pause,
        ManualOverlayController manual,
        MapOverlayController map)
    {
        manual.SetOpen(true);
        ApplyInputLock(player, pause, manual, map);
    }

    public static bool HandleCancel(
        PlayerController player,
        PauseOverlayController pause,
        ManualOverlayController manual,
        MapOverlayController map)
    {
        if (manual.Visible)
        {
            manual.SetOpen(false);
            ApplyInputLock(player, pause, manual, map);
            return true;
        }

        if (map.Visible)
        {
            map.Visible = false;
            ApplyInputLock(player, pause, manual, map);
            return true;
        }

        if (pause.Visible)
        {
            pause.SetOverlayVisible(false);
            ApplyInputLock(player, pause, manual, map);
            return true;
        }

        return false;
    }

    public static bool CanOpenManualFromPanel(PlayerController player, Node3D? manualPanel)
    {
        return manualPanel is not null && player.GlobalPosition.DistanceTo(manualPanel.GlobalPosition) <= 2.8f;
    }
}
