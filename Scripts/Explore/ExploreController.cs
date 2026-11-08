using Godot;

public partial class ExploreController : Node3D
{
    private ExploreRuntimeCoordinator? _runtime;

    public override void _Ready()
    {
        var player = GetNode<PlayerController>("Player");
        var battlePresentation = new InWorldBattlePresentationController(
            GetNode<Node3D>("BattlePresentationRoot"),
            GetNode<Camera3D>("BattleCamera"),
            player);
        _runtime = new ExploreRuntimeCoordinator(
            GetTree(),
            this,
            GetNode<GameSession>("/root/GameSession"),
            GetNode<SaveService>("/root/SaveService"),
            player,
            GetNode<Node3D>("DungeonRoot"),
            GetNode<Node3D>("EnemiesRoot"),
            GetNode<PauseOverlayController>("CanvasLayer/PauseOverlay"),
            GetNode<ManualOverlayController>("CanvasLayer/ManualOverlay"),
            GetNode<MapOverlayController>("CanvasLayer/MapOverlay"),
            GetNode<HudOverlayController>("CanvasLayer/HudOverlay"),
            GetNode<BattleController>("CanvasLayer/BattleOverlay"),
            GetNode<RewardController>("CanvasLayer/RewardOverlay"),
            battlePresentation,
            new ExploreSceneAssembler(),
            new ExploreWorldInteractionService(),
            new ExploreTransitionFlow());
        _runtime.Initialize();
    }

    public override void _PhysicsProcess(double deltaSeconds)
    {
        _runtime?.Tick(deltaSeconds);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        _runtime?.HandleInput(@event);
    }
}
