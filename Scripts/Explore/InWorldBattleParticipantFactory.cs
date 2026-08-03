using Godot;

public static class InWorldBattleParticipantFactory
{
    private static readonly EnemyModelVisualFactory EnemyFactory = new();

    public static InWorldBattleParticipant CreatePlayer(Node3D parent)
    {
        var root = new Node3D { Name = "PlayerParticipant" };
        var visualRoot = CreateVisualRoot();
        var sprite = new Sprite3D
        {
            Texture = GameAssets.LoadPlayerSprite(),
            PixelSize = 0.0085f,
            Billboard = BaseMaterial3D.BillboardModeEnum.Disabled,
            Position = new Vector3(0f, 1.0f, 0f),
        };
        visualRoot.GetNode<Node3D>("VisualPivot/ImportedModel").AddChild(sprite);
        root.AddChild(visualRoot);
        root.AddChild(CreateTargetRing());
        parent.AddChild(root);
        return new InWorldBattleParticipant(
            CombatService.PlayerActorId,
            root,
            visualRoot.GetNode<Node3D>("VisualPivot"),
            visualRoot.GetNode<Node3D>("VisualPivot/ImportedModel"),
            root.GetNode<MeshInstance3D>("TargetRing"),
            EnemyVisualAnimationProfiles.Resolve("player"),
            false,
            0f,
            0f);
    }

    public static InWorldBattleParticipant CreateEnemy(Node3D parent, OverworldEnemyModel enemy)
    {
        var root = new Node3D { Name = $"EnemyParticipant_{enemy.EnemyId}" };
        var visualRoot = EnemyFactory.Create(enemy.Sprite) ?? CreateFallbackEnemyVisual(enemy);
        root.AddChild(visualRoot);
        root.AddChild(CreateTargetRing());
        parent.AddChild(root);

        var binding = EnemyModelLibrary.GetVisualProfileOrDefault(enemy.Sprite);
        return new InWorldBattleParticipant(
            enemy.EnemyId,
            root,
            visualRoot.GetNode<Node3D>("VisualPivot"),
            visualRoot.GetNode<Node3D>("VisualPivot/ImportedModel"),
            root.GetNode<MeshInstance3D>("TargetRing"),
            EnemyVisualAnimationProfiles.Resolve(enemy.Sprite, enemy.Name, enemy.SpecName),
            binding.UseWingFlap,
            binding.WingFlapSpeedHz,
            binding.WingFlapAngleDeg);
    }

    private static Node3D CreateFallbackEnemyVisual(OverworldEnemyModel enemy)
    {
        var visualRoot = CreateVisualRoot();
        var sprite = new Sprite3D
        {
            Texture = GameAssets.LoadEnemySprite(enemy.Sprite),
            PixelSize = 0.0085f,
            Billboard = BaseMaterial3D.BillboardModeEnum.Disabled,
            Position = new Vector3(0f, 1.0f, 0f),
        };
        visualRoot.GetNode<Node3D>("VisualPivot/ImportedModel").AddChild(sprite);
        return visualRoot;
    }

    private static Node3D CreateVisualRoot()
    {
        var visualRoot = new Node3D { Name = "VisualRoot" };
        var visualPivot = new Node3D { Name = "VisualPivot" };
        var importedModel = new Node3D { Name = "ImportedModel" };
        visualRoot.AddChild(visualPivot);
        visualPivot.AddChild(importedModel);
        return visualRoot;
    }

    private static MeshInstance3D CreateTargetRing()
    {
        return new MeshInstance3D
        {
            Name = "TargetRing",
            Visible = false,
            Position = new Vector3(0f, 0.04f, 0f),
            Mesh = new CylinderMesh { TopRadius = 0.82f, BottomRadius = 0.82f, Height = 0.03f, RadialSegments = 24 },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.92f, 0.84f, 0.34f, 0.95f),
                EmissionEnabled = true,
                Emission = new Color(0.9f, 0.82f, 0.28f),
                EmissionEnergyMultiplier = 1.6f,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            },
        };
    }
}
