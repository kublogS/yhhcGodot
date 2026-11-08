using Godot;

public partial class NameEntryController : Control
{
    private GameSession _session = null!;
    private SaveService _saveService = null!;
    private LineEdit _nameEdit = null!;
    private Label _status = null!;
    private NewGameFlowCoordinator _flow = null!;

    public override void _Ready()
    {
        _session = GetNode<GameSession>("/root/GameSession");
        _saveService = GetNode<SaveService>("/root/SaveService");
        _flow = new NewGameFlowCoordinator(_session, _saveService);
        _nameEdit = GetNode<LineEdit>("Center/VBox/NameEdit");
        _status = GetNodeOrNull<Label>("Center/VBox/Status") ?? CreateStatusLabel();
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
        _status.Text = string.Empty;
        var result = _flow.StartNewGame(_nameEdit.Text);
        if (!result.Success)
        {
            _status.Text = result.Message;
            return;
        }

        SceneRouteNavigator.Navigate(result.Route, GetTree());
    }

    private Label CreateStatusLabel()
    {
        var label = new Label
        {
            Name = "Status",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Modulate = new Color(1f, 0.78f, 0.78f),
        };
        GetNode<VBoxContainer>("Center/VBox").AddChild(label);
        return label;
    }
}
