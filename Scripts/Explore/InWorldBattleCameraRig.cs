using Godot;

public sealed class InWorldBattleCameraRig
{
    private readonly Camera3D _battleCamera;
    private readonly Camera3D _exploreCamera;
    private Vector3 _cameraPosition;
    private Vector3 _lookTarget;
    private Vector3 _desiredLookTarget;

    public InWorldBattleCameraRig(Camera3D battleCamera, Camera3D exploreCamera)
    {
        _battleCamera = battleCamera;
        _exploreCamera = exploreCamera;
    }

    public void Activate(Vector3 cameraPosition, Vector3 lookTarget)
    {
        _cameraPosition = cameraPosition;
        _lookTarget = lookTarget;
        _desiredLookTarget = lookTarget;
        _exploreCamera.Current = false;
        _battleCamera.GlobalPosition = _cameraPosition;
        _battleCamera.LookAt(_lookTarget, Vector3.Up);
        _battleCamera.Current = true;
    }

    public void SetBasePose(Vector3 cameraPosition, Vector3 lookTarget)
    {
        _cameraPosition = cameraPosition;
        _desiredLookTarget = lookTarget;
    }

    public void Focus(Vector3 lookTarget)
    {
        _desiredLookTarget = lookTarget;
    }

    public void Tick(float dt)
    {
        if (!_battleCamera.Current || dt <= 0f)
        {
            return;
        }

        _lookTarget = _lookTarget.Lerp(_desiredLookTarget, Mathf.Clamp(dt * 5f, 0f, 1f));
        _battleCamera.GlobalPosition = _battleCamera.GlobalPosition.Lerp(_cameraPosition, Mathf.Clamp(dt * 4f, 0f, 1f));
        _battleCamera.LookAt(_lookTarget, Vector3.Up);
    }

    public void Deactivate()
    {
        _battleCamera.Current = false;
        _exploreCamera.Current = true;
    }
}
