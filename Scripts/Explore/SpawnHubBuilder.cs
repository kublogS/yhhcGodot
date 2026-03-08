using Godot;

public sealed class SpawnHubLayout
{
    public int WidthTiles { get; init; } = 16;
    public int HeightTiles { get; init; } = 16;
    public Vector3 PlayerSpawn { get; init; } = new(0f, 1.2f, -10f);
    public Vector3 PosterAnchor { get; init; } = new(0f, 2.1f, -15.45f);
    public Vector3 PosterNormal { get; init; } = Vector3.Back;
    public Vector3 NorthDoorInteractPoint { get; init; } = new(0f, 1.2f, 13.5f);
    public float NorthDoorInteractRadius { get; init; } = 2.4f;
}

public static class SpawnHubBuilder
{
    private const int GridSize = 16;
    private const float TileSize = 2f;
    private const float FloorThickness = 0.2f;
    private const float WallThickness = 0.45f;
    private const float WallHeight = 6.9f;
    private const float DoorWidth = 4.2f;
    private const float DoorClearHeight = 4.4f;

    public static SpawnHubLayout Build(Node3D root)
    {
        Clear(root);
        var floorColor = MakeMaterial(PythonColorPalette.FloorLightGray);
        var wallColor = MakeMaterial(PythonColorPalette.GrayDark);
        var doorColor = MakeMaterial(PythonColorPalette.Door);
        var portalColor = MakeMaterial(PythonColorPalette.WithAlpha(PythonColorPalette.Gray, 190));
        portalColor.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;

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

        AddVisual(root, new Vector3(DoorWidth - 0.35f, DoorClearHeight - 0.6f, 0.03f), new Vector3(0f, (DoorClearHeight * 0.5f) - 0.25f, northZ - 0.22f), portalColor);
        AddVisual(root, new Vector3(0.24f, DoorClearHeight - 0.2f, 0.24f), new Vector3(-(DoorWidth * 0.5f) + 0.12f, (DoorClearHeight * 0.5f) - 0.1f, northZ - 0.1f), doorColor);
        AddVisual(root, new Vector3(0.24f, DoorClearHeight - 0.2f, 0.24f), new Vector3((DoorWidth * 0.5f) - 0.12f, (DoorClearHeight * 0.5f) - 0.1f, northZ - 0.1f), doorColor);
        AddVisual(root, new Vector3(DoorWidth, 0.24f, 0.24f), new Vector3(0f, DoorClearHeight - 0.12f, northZ - 0.1f), doorColor);

        return new SpawnHubLayout
        {
            WidthTiles = GridSize,
            HeightTiles = GridSize,
            PlayerSpawn = new Vector3(0f, 1.2f, -half + (TileSize * 3f)),
            PosterAnchor = new Vector3(0f, 2.0f, -innerEdge + 0.22f),
            PosterNormal = Vector3.Back,
            NorthDoorInteractPoint = new Vector3(0f, 1.2f, innerEdge - 1.7f),
            NorthDoorInteractRadius = 2.5f,
        };
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
