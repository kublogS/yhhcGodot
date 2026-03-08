using Godot;

public partial class ExploreController
{
    private void HandleWorldInteractInput()
    {
        if (_pause.Visible || _manual.Visible)
        {
            return;
        }

        if (_inSpawnHub)
        {
            if (CanUseSpawnDoor())
            {
                EnterGeneratedOverworld();
                return;
            }

            if (CanOpenManualFromPanel())
            {
                _manual.SetOpen(true);
                _player.SetLookEnabled(false);
            }

            return;
        }

        TryBreakFacingBreakable();
    }

    private bool CanUseSpawnDoor()
    {
        return _player.GlobalPosition.DistanceTo(_spawnDoorInteractPoint) <= _spawnDoorInteractRadius;
    }
}
