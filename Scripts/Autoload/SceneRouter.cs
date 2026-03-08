using Godot;

public partial class SceneRouter : Node
{
    public static SceneRouter Instance { get; private set; } = null!;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public void GoToMainMenu() => Change("res://Scenes/MainMenu.tscn");
    public void GoToSavesMenu() => Change("res://Scenes/SavesMenu.tscn");
    public void GoToNameEntry() => Change("res://Scenes/NameEntry.tscn");
    public void GoToExplore() => Change("res://Scenes/Explore.tscn");
    public void GoToBattle() => Change("res://Scenes/Battle.tscn");
    public void GoToReward() => Change("res://Scenes/Reward.tscn");

    private void Change(string scenePath)
    {
        var err = GetTree().ChangeSceneToFile(scenePath);
        if (err != Error.Ok)
        {
            GD.PrintErr($"Cambio scena fallito: {scenePath} ({err})");
        }
    }
}
