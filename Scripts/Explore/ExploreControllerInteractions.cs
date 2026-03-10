using Godot;

public partial class ExploreController
{
    private const float ClickInteractDistance = 6f;

    private void HandleLeftMouseInteraction(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouse || !mouse.Pressed || mouse.ButtonIndex != MouseButton.Left)
        {
            return;
        }

        if (IsInteractionModalActive())
        {
            return;
        }

        var hit = WorldInteractionRaycaster.Raycast(_player.GetViewCamera(), ClickInteractDistance);
        if (WorldInteractionRaycaster.HasGroupInHierarchy(hit, WorldInteractionGroups.ManualSheet))
        {
            OpenManualOverlay();
        }
    }

    private void HandleWorldInteractInput()
    {
        if (IsInteractionModalActive())
        {
            return;
        }

        if (_inSpawnHub)
        {
            if (CanOpenManualFromPanel())
            {
                OpenManualOverlay();
            }

            return;
        }

        if (TryUseSaveSanctuary())
        {
            return;
        }

        TryBreakFacingBreakable();
    }

    private void HandleSpawnPortalCrossing()
    {
        if (!_inSpawnHub || IsInteractionModalActive() || _spawnPortalTransitionLock)
        {
            return;
        }

        var pos = _player.GlobalPosition;
        if (Mathf.Abs(pos.X) > _spawnPortalEntryHalfWidth || pos.Z < _spawnPortalEntryMinZ)
        {
            return;
        }

        _spawnPortalTransitionLock = true;
        EnterGeneratedOverworld();
    }

    private void OpenManualOverlay()
    {
        _manual.SetOpen(true);
        ApplyInteractionInputLock();
    }

    private bool IsInteractionModalActive()
    {
        return _pause.Visible || _manual.Visible || _map.Visible;
    }

    private void ApplyInteractionInputLock()
    {
        var interactive = !IsInteractionModalActive();
        _player.SetLookEnabled(interactive);
        _player.SetMovementEnabled(interactive);
    }

    private bool HandleInteractionCancelRequest()
    {
        if (_manual.Visible)
        {
            _manual.SetOpen(false);
            ApplyInteractionInputLock();
            return true;
        }

        if (_map.Visible)
        {
            _map.Visible = false;
            ApplyInteractionInputLock();
            return true;
        }

        if (_pause.Visible)
        {
            _pause.SetOverlayVisible(false);
            ApplyInteractionInputLock();
            return true;
        }

        return false;
    }
}
