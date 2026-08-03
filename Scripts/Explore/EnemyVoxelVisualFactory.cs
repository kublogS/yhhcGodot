using Godot;

public sealed class EnemyVoxelVisualFactory
{
    private const string ShaderPath = "res://Assets/shaders/enemy_ascii_voxel.gdshader";
    private const float HeightOffset = -0.6f;

    private readonly PngImageLoader _imageLoader = new();
    private static ShaderMaterial? _sharedMaterial;

    public Node3D? Create(string? spriteId)
    {
        var spritePath = GameAssets.ResolveEnemySpritePath(spriteId);
        var image = _imageLoader.Load(spritePath);
        var mesh = SpritePixelMeshFactory.Build(image);
        if (mesh is null)
        {
            return null;
        }

        var meshInstance = new MeshInstance3D
        {
            Name = "VoxelMesh",
            Mesh = mesh,
            MaterialOverride = GetOrCreateMaterial(),
        };

        var importedModel = new Node3D { Name = "ImportedModel" };
        importedModel.AddChild(meshInstance);

        var visualPivot = new Node3D
        {
            Name = "VisualPivot",
            Position = new Vector3(0f, HeightOffset, 0f),
        };
        visualPivot.AddChild(importedModel);

        var visualRoot = new Node3D { Name = "VisualRoot" };
        visualRoot.AddChild(visualPivot);
        return visualRoot;
    }

    private static ShaderMaterial GetOrCreateMaterial()
    {
        if (_sharedMaterial is not null)
        {
            return _sharedMaterial;
        }

        var shader = GD.Load<Shader>(ShaderPath);
        _sharedMaterial = new ShaderMaterial { Shader = shader };
        return _sharedMaterial;
    }
}
