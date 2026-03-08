using Godot;

public static class DungeonBuilder
{
    public static float TileSize = 2.0f;
    private const float WallHeight = 6.6f;
    private const float WallColliderInset = 0.24f;

    public static void Build(Node3D root, DungeonData dungeon)
    {
        Clear(root);
        var floorMesh = new BoxMesh { Size = new Vector3(TileSize, 0.1f, TileSize) };
        var wallMesh = new BoxMesh { Size = new Vector3(TileSize, WallHeight, TileSize) };
        var wallShape = new BoxShape3D
        {
            Size = new Vector3(TileSize - (WallColliderInset * 2f), WallHeight, TileSize - (WallColliderInset * 2f)),
        };

        var floorMaterial = MakeMaterial(PythonColorPalette.FloorLightGray);
        var wallMaterial = MakeMaterial(PythonColorPalette.GrayLight);
        var doorMaterial = MakeMaterial(PythonColorPalette.Door);
        var exitMaterial = MakeMaterial(PythonColorPalette.WithAlpha(PythonColorPalette.Title, 205));
        ((StandardMaterial3D)exitMaterial).Transparency = BaseMaterial3D.TransparencyEnum.Alpha;

        for (var y = 0; y < dungeon.Height; y++)
        {
            for (var x = 0; x < dungeon.Width; x++)
            {
                var tile = dungeon.GetTile(x, y);
                var pos = DungeonGenerator.GridToWorld(x, y, TileSize);
                if (tile is TileType.Floor or TileType.Door or TileType.Exit or TileType.Breakable)
                {
                    AddMesh(root, floorMesh, floorMaterial, pos + new Vector3(0f, -0.05f, 0f));
                }

                if (tile == TileType.Wall)
                {
                    AddWall(root, wallMesh, wallMaterial, wallShape, pos + new Vector3(0f, WallHeight * 0.5f - 0.1f, 0f));
                    continue;
                }

                if (tile == TileType.Door)
                {
                    AddDoorway(root, dungeon, x, y, pos, doorMaterial);
                }
                else if (tile == TileType.Exit)
                {
                    AddExitMarker(root, pos, exitMaterial);
                }
            }
        }
    }

    private static StandardMaterial3D MakeMaterial(Color color)
    {
        return new StandardMaterial3D { AlbedoColor = color, Roughness = 1.0f, Metallic = 0f };
    }

    private static void AddMesh(Node3D root, Mesh mesh, Material material, Vector3 pos)
    {
        root.AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Position = pos });
    }

    private static void AddWall(Node3D root, Mesh mesh, Material material, Shape3D shape, Vector3 pos)
    {
        var wall = new Node3D { Position = pos };
        wall.AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = material });
        var body = new StaticBody3D();
        body.AddChild(new CollisionShape3D { Shape = shape });
        wall.AddChild(body);
        root.AddChild(wall);
    }

    private static void AddDoorway(Node3D root, DungeonData dungeon, int x, int y, Vector3 pos, Material material)
    {
        var opensX = dungeon.IsWalkable(x - 1, y) && dungeon.IsWalkable(x + 1, y);
        if (opensX)
        {
            AddDoorPart(root, material, new Vector3(0.18f, 2.8f, 0.24f), pos + new Vector3(-0.86f, 1.4f, 0f));
            AddDoorPart(root, material, new Vector3(0.18f, 2.8f, 0.24f), pos + new Vector3(0.86f, 1.4f, 0f));
            AddDoorPart(root, material, new Vector3(1.9f, 0.3f, 0.24f), pos + new Vector3(0f, 2.8f, 0f));
            return;
        }

        AddDoorPart(root, material, new Vector3(0.24f, 2.8f, 0.18f), pos + new Vector3(0f, 1.4f, -0.86f));
        AddDoorPart(root, material, new Vector3(0.24f, 2.8f, 0.18f), pos + new Vector3(0f, 1.4f, 0.86f));
        AddDoorPart(root, material, new Vector3(0.24f, 0.3f, 1.9f), pos + new Vector3(0f, 2.8f, 0f));
    }

    private static void AddExitMarker(Node3D root, Vector3 pos, Material material)
    {
        AddDoorPart(root, material, new Vector3(0.18f, 2.4f, 0.18f), pos + new Vector3(-0.62f, 1.2f, 0f));
        AddDoorPart(root, material, new Vector3(0.18f, 2.4f, 0.18f), pos + new Vector3(0.62f, 1.2f, 0f));
        AddDoorPart(root, material, new Vector3(1.35f, 0.18f, 0.18f), pos + new Vector3(0f, 2.35f, 0f));
        AddDoorPart(root, material, new Vector3(1.1f, 2.0f, 0.06f), pos + new Vector3(0f, 1.0f, -0.02f));
    }

    private static void AddDoorPart(Node3D root, Material material, Vector3 size, Vector3 position)
    {
        root.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, MaterialOverride = material, Position = position });
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
