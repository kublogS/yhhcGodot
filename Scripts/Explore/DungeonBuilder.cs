using Godot;

public static class DungeonBuilder
{
    public static float TileSize = 2.0f;
    private const float WallHeight = 2.2f;
    private const float WallColliderInset = 0.24f;

    public static void Build(Node3D root, DungeonData dungeon)
    {
        Clear(root);
        var floorMesh = new BoxMesh { Size = new Vector3(TileSize, 0.1f, TileSize) };
        var wallMesh = new BoxMesh { Size = new Vector3(TileSize, WallHeight, TileSize) };
        var doorMesh = new BoxMesh { Size = new Vector3(TileSize, 1.7f, TileSize * 0.3f) };
        var wallShape = new BoxShape3D
        {
            Size = new Vector3(TileSize - (WallColliderInset * 2f), WallHeight, TileSize - (WallColliderInset * 2f)),
        };

        var floorMaterial = MakeMaterial(new Color(0.25f, 0.28f, 0.25f));
        var wallMaterial = MakeMaterial(new Color(0.55f, 0.55f, 0.58f));
        var doorMaterial = MakeMaterial(new Color(0.45f, 0.31f, 0.18f));

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

                if (tile is TileType.Wall)
                {
                    AddWall(root, wallMesh, wallMaterial, wallShape, pos + new Vector3(0f, WallHeight * 0.5f - 0.1f, 0f));
                    continue;
                }

                if (tile is TileType.Door)
                {
                    AddMesh(root, doorMesh, doorMaterial, pos + new Vector3(0f, 0.85f, 0f));
                }
            }
        }
    }

    private static StandardMaterial3D MakeMaterial(Color color)
    {
        return new StandardMaterial3D
        {
            AlbedoColor = color,
            Roughness = 1.0f,
            Metallic = 0f,
        };
    }

    private static void AddMesh(Node3D root, Mesh mesh, Material material, Vector3 pos)
    {
        var node = new MeshInstance3D
        {
            Mesh = mesh,
            MaterialOverride = material,
            Position = pos,
        };
        root.AddChild(node);
    }

    private static void AddWall(Node3D root, Mesh mesh, Material material, Shape3D shape, Vector3 pos)
    {
        var wall = new Node3D
        {
            Position = pos,
        };

        wall.AddChild(new MeshInstance3D
        {
            Mesh = mesh,
            MaterialOverride = material,
        });

        var body = new StaticBody3D();
        body.AddChild(new CollisionShape3D { Shape = shape });
        wall.AddChild(body);
        root.AddChild(wall);
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
