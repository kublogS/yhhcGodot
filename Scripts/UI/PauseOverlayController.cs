using Godot;

public partial class PauseOverlayController : Control
{
    public override void _Ready()
    {
        Visible = false;
    }

    public void SetOverlayVisible(bool open)
    {
        Visible = open;
    }
}
