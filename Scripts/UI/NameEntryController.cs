using Godot;

public partial class NameEntryController : Control
{
    private LineEdit _nameEdit = null!;
    private NewGameFlowCoordinator _flow = null!;

    public override void _Ready()
    {
        _flow = new NewGameFlowCoordinator(GameSession.Instance, SaveService.Instance);
        _nameEdit = GetNode<LineEdit>("Center/VBox/NameEdit");
        GetNode<Button>("Center/VBox/StartButton").Pressed += OnStart;
        GetNode<Button>("Center/VBox/BackButton").Pressed += () => SceneRouteNavigator.Navigate(SceneRoute.SavesMenu, GetTree());
        _nameEdit.GrabFocus();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_accept"))
        {
            OnStart();
        }
        else if (@event.IsActionPressed("ui_cancel"))
        {
            SceneRouteNavigator.Navigate(SceneRoute.SavesMenu, GetTree());
        }
    }

    private void OnStart()
    {
        SceneRouteNavigator.Navigate(_flow.StartNewGame(_nameEdit.Text), GetTree());
    }
}
