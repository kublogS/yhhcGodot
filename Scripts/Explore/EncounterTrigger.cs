using Godot;

public partial class EncounterTrigger : Area3D
{
    [Signal] public delegate void EncounterStartedEventHandler(Node source);

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        EmitSignal(SignalName.EncounterStarted, body);
    }
}
