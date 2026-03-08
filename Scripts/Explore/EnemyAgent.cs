using Godot;

public partial class EnemyAgent : CharacterBody3D
{
    public OverworldEnemyModel Model { get; private set; } = new();
    private static readonly EnemyAsciiBillboardFactory AsciiFactory = new();

    private PlayerController _player = null!;
    private DungeonData _dungeon = null!;
    private float _alertTimer;

    public void Setup(OverworldEnemyModel model, PlayerController player, DungeonData dungeon)
    {
        Model = model;
        _player = player;
        _dungeon = dungeon;
        GlobalPosition = new Vector3(model.X, 0.75f, model.Y);

        foreach (var child in GetChildren())
        {
            if (child is Node node)
            {
                node.QueueFree();
            }
        }

        var spritePath = GameAssets.ResolveEnemySpritePath(Model.Sprite);
        var asciiBillboard = AsciiFactory.Create(spritePath);
        if (asciiBillboard is not null)
        {
            AddChild(asciiBillboard);
        }
        else
        {
            var body = new MeshInstance3D
            {
                Mesh = new CapsuleMesh { Radius = 0.4f, Height = 1.2f },
                MaterialOverride = new StandardMaterial3D { AlbedoColor = PythonColorPalette.Red },
            };
            AddChild(body);
        }

        var collision = new CollisionShape3D
        {
            Shape = new CapsuleShape3D { Radius = 0.35f, Height = 1.2f },
            Position = new Vector3(0f, 0.75f, 0f),
        };
        AddChild(collision);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player is null || _dungeon is null || !Model.Active)
        {
            return;
        }

        var dt = (float)delta;
        _alertTimer = Mathf.Max(0f, _alertTimer - dt);
        OverworldAI.TickEnemyRuntime(Model, dt);
        var grid = _dungeon.Grid;
        var (vxPs, vyPs, chasing) = OverworldAI.ComputeVelocity(Model, _player.GlobalPosition.X, _player.GlobalPosition.Z, grid, _alertTimer);
        var motion = new Vector3(vxPs, 0f, vyPs) * dt;
        Velocity = motion / dt;

        var oldPos = GlobalPosition;
        MoveAndSlide();
        var moved = oldPos.DistanceTo(GlobalPosition) > 0.0005f;
        OverworldAI.MarkMoveResult(Model, moved, dt);

        Model.X = GlobalPosition.X;
        Model.Y = GlobalPosition.Z;

        if (chasing)
        {
            _alertTimer = 1.5f;
        }
    }
}
