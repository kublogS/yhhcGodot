using Godot;

public sealed class SpawnHubLayout
{
    public int WidthTiles { get; init; } = 16;
    public int HeightTiles { get; init; } = 16;
    public Vector3 PlayerSpawn { get; init; } = new(0f, 1.2f, -10f);
    public Vector3 PosterAnchor { get; init; } = new(0f, 2.1f, -15.45f);
    public Vector3 PosterNormal { get; init; } = Vector3.Back;
    public float PortalEntryMinZ { get; init; } = 0f;
    public float PortalEntryHalfWidth { get; init; } = 1.8f;
}

public static class SpawnHubBuilder
{
    private const int GridSize = 16;
    private const float TileSize = 2f;
    private const float FloorThickness = 0.2f;
    private const float WallThickness = 0.45f;
    private const float WallHeight = 6.9f;
    private const float DoorWidth = 8.4f;
    private const float DoorClearHeight = 5.8f;

    public static SpawnHubLayout Build(Node3D root)
    {
        Clear(root);
        var floorColor = MakeMaterial(PythonColorPalette.FloorLightGray);
        var wallColor = DungeonMaterialFactory.CreateHubWallMaterial();
        var frameColor = DungeonMaterialFactory.CreateObsidianFrameMaterial();
        var insetColor = DungeonMaterialFactory.CreateObsidianInsetMaterial();
        var edgeColor = DungeonMaterialFactory.CreatePortalEdgeMaterial();

        var mapSize = GridSize * TileSize;
        var half = mapSize * 0.5f;
        var innerEdge = half - (WallThickness * 0.5f);
        var wallY = (WallHeight * 0.5f) - (FloorThickness * 0.5f);

        AddSolid(root, new Vector3(mapSize, FloorThickness, mapSize), new Vector3(0f, -FloorThickness * 0.5f, 0f), floorColor);
        AddSolid(root, new Vector3(mapSize, WallHeight, WallThickness), new Vector3(0f, wallY, -innerEdge), wallColor);
        AddSolid(root, new Vector3(WallThickness, WallHeight, mapSize), new Vector3(-innerEdge, wallY, 0f), wallColor);
        AddSolid(root, new Vector3(WallThickness, WallHeight, mapSize), new Vector3(innerEdge, wallY, 0f), wallColor);

        var segmentWidth = (mapSize - DoorWidth) * 0.5f;
        var northZ = innerEdge;
        AddSolid(root, new Vector3(segmentWidth, WallHeight, WallThickness), new Vector3(-(DoorWidth * 0.5f) - (segmentWidth * 0.5f), wallY, northZ), wallColor);
        AddSolid(root, new Vector3(segmentWidth, WallHeight, WallThickness), new Vector3((DoorWidth * 0.5f) + (segmentWidth * 0.5f), wallY, northZ), wallColor);

        var lintelHeight = WallHeight - DoorClearHeight;
        var lintelY = (DoorClearHeight + (lintelHeight * 0.5f)) - (FloorThickness * 0.5f);
        AddSolid(root, new Vector3(DoorWidth, lintelHeight, WallThickness), new Vector3(0f, lintelY, northZ), wallColor);

        var portalSize = new Vector2(DoorWidth - 1.2f, DoorClearHeight - 1.0f);
        var portalPosition = new Vector3(0f, (DoorClearHeight * 0.5f) - 0.18f, northZ - 0.28f);
        AddPortal(root, portalSize, portalPosition);
        BuildPortalFrame(root, northZ, frameColor, insetColor, edgeColor);
        AddPortalLights(root, northZ);

        return new SpawnHubLayout
        {
            WidthTiles = GridSize,
            HeightTiles = GridSize,
            PlayerSpawn = new Vector3(0f, 1.2f, TileSize * 2f),
            PosterAnchor = new Vector3(0f, 2.0f, -innerEdge + 0.22f),
            PosterNormal = Vector3.Back,
            PortalEntryMinZ = northZ - (WallThickness * 0.5f) + 0.03f,
            PortalEntryHalfWidth = (DoorWidth * 0.5f) - 0.6f,
        };
    }

    private static void BuildPortalFrame(Node3D root, float northZ, Material frameMaterial, Material insetMaterial, Material edgeMaterial)
    {
        var columnX = (DoorWidth * 0.5f) - 0.38f;
        var midY = (DoorClearHeight * 0.5f) + 0.08f;
        AddVisual(root, new Vector3(0.86f, DoorClearHeight + 1.05f, 0.88f), new Vector3(-columnX, midY, northZ + 0.16f), frameMaterial);
        AddVisual(root, new Vector3(0.86f, DoorClearHeight + 1.05f, 0.88f), new Vector3(columnX, midY, northZ + 0.16f), frameMaterial);
        AddVisual(root, new Vector3(1.36f, DoorClearHeight + 1.24f, 0.26f), new Vector3(-columnX, midY, northZ + 0.48f), frameMaterial);
        AddVisual(root, new Vector3(1.36f, DoorClearHeight + 1.24f, 0.26f), new Vector3(columnX, midY, northZ + 0.48f), frameMaterial);
        AddVisual(root, new Vector3(1.18f, DoorClearHeight + 0.58f, 0.34f), new Vector3(-columnX, midY + 0.03f, northZ - 0.06f), insetMaterial);
        AddVisual(root, new Vector3(1.18f, DoorClearHeight + 0.58f, 0.34f), new Vector3(columnX, midY + 0.03f, northZ - 0.06f), insetMaterial);
        AddVisual(root, new Vector3(DoorWidth + 0.9f, 0.84f, 0.92f), new Vector3(0f, DoorClearHeight + 0.12f, northZ + 0.18f), frameMaterial);
        AddVisual(root, new Vector3(DoorWidth + 1.45f, 0.42f, 0.7f), new Vector3(0f, DoorClearHeight + 0.58f, northZ + 0.1f), frameMaterial);
        AddVisual(root, new Vector3(DoorWidth + 2.0f, 0.24f, 0.44f), new Vector3(0f, DoorClearHeight + 0.86f, northZ + 0.18f), frameMaterial);
        AddVisual(root, new Vector3(DoorWidth - 0.28f, 0.34f, 0.46f), new Vector3(0f, DoorClearHeight + 0.3f, northZ - 0.12f), insetMaterial);
        AddVisual(root, new Vector3(1.24f, 1.02f, 0.58f), new Vector3(0f, DoorClearHeight + 0.66f, northZ + 0.08f), frameMaterial);
        AddVisual(root, new Vector3(0.58f, 0.42f, 0.2f), new Vector3(0f, DoorClearHeight + 0.88f, northZ - 0.16f), insetMaterial);
        AddVisual(root, new Vector3(2.18f, 0.24f, 0.36f), new Vector3(0f, DoorClearHeight + 1.12f, northZ + 0.06f), frameMaterial);
        AddVisual(root, new Vector3(DoorWidth - 0.62f, 0.5f, 0.5f), new Vector3(0f, 0.18f, northZ + 0.1f), frameMaterial);
        AddVisual(root, new Vector3(DoorWidth - 1.52f, DoorClearHeight - 0.76f, 0.18f), new Vector3(0f, (DoorClearHeight * 0.5f) - 0.16f, northZ - 0.12f), insetMaterial);
        AddVisual(root, new Vector3(DoorWidth - 1.22f, 0.1f, 0.1f), new Vector3(0f, DoorClearHeight - 0.12f, northZ - 0.19f), edgeMaterial);
        AddVisual(root, new Vector3(0.1f, DoorClearHeight - 0.98f, 0.1f), new Vector3(-(DoorWidth * 0.5f) + 0.68f, (DoorClearHeight * 0.5f) - 0.12f, northZ - 0.19f), edgeMaterial);
        AddVisual(root, new Vector3(0.1f, DoorClearHeight - 0.98f, 0.1f), new Vector3((DoorWidth * 0.5f) - 0.68f, (DoorClearHeight * 0.5f) - 0.12f, northZ - 0.19f), edgeMaterial);
        AddVisual(root, new Vector3(DoorWidth - 1.22f, 0.1f, 0.1f), new Vector3(0f, 0.38f, northZ - 0.19f), edgeMaterial);
    }

    private static void AddPortalLights(Node3D root, float northZ)
    {
        AddPortalLight(root, new Vector3(-(DoorWidth * 0.5f) + 1.05f, DoorClearHeight * 0.74f, northZ - 0.34f));
        AddPortalLight(root, new Vector3((DoorWidth * 0.5f) - 1.05f, DoorClearHeight * 0.74f, northZ - 0.34f));
    }

    private static void AddPortalLight(Node3D root, Vector3 position)
    {
        root.AddChild(new OmniLight3D
        {
            Position = position,
            LightColor = new Color(0.62f, 0.77f, 0.92f, 1f),
            LightEnergy = 0.38f,
            LightSpecular = 0.85f,
            ShadowEnabled = false,
            OmniRange = 6.5f,
        });
    }

    private static void AddSolid(Node3D root, Vector3 size, Vector3 position, Material material)
    {
        var node = new Node3D { Position = position };
        node.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, MaterialOverride = material });
        var body = new StaticBody3D();
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
        node.AddChild(body);
        root.AddChild(node);
    }

    private static void AddVisual(Node3D root, Vector3 size, Vector3 position, Material material)
    {
        root.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, Position = position, MaterialOverride = material });
    }

    private static void AddPortal(Node3D root, Vector2 size, Vector3 position)
    {
        var panel = PortalVisualFactory.Create(size);
        panel.Position = position;
        root.AddChild(panel);
    }

    private static StandardMaterial3D MakeMaterial(Color color)
    {
        return new StandardMaterial3D { AlbedoColor = color, Roughness = 1f, Metallic = 0f };
    }

    private static void Clear(Node3D root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is Node node)
            {
                node.QueueFree();
            }
        }
    }
}
