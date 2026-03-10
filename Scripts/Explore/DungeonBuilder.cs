using System.Collections.Generic;
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
        var saveMaterial = MakeMaterial(new Color(0.84f, 0.9f, 1f, 1f));
        var lampMaterial = MakeMaterial(new Color(0.72f, 0.75f, 0.8f, 1f));
        var exitMaterial = MakeMaterial(PythonColorPalette.WithAlpha(PythonColorPalette.Title, 205));
        ((StandardMaterial3D)exitMaterial).Transparency = BaseMaterial3D.TransparencyEnum.Alpha;

        for (var y = 0; y < dungeon.Height; y++)
        {
            for (var x = 0; x < dungeon.Width; x++)
            {
                var tile = dungeon.GetTile(x, y);
                var pos = DungeonGenerator.GridToWorld(x, y, TileSize);
                if (tile is TileType.Floor or TileType.Doorway or TileType.Threshold or TileType.Exit or TileType.Breakable or TileType.Save)
                {
                    AddMesh(root, floorMesh, floorMaterial, pos + new Vector3(0f, -0.05f, 0f));
                }

                if (tile == TileType.Wall)
                {
                    AddWall(root, wallMesh, wallMaterial, wallShape, pos + new Vector3(0f, WallHeight * 0.5f - 0.1f, 0f));
                    continue;
                }

                if (tile == TileType.Exit)
                {
                    AddExitMarker(root, pos, exitMaterial);
                }
                else if (tile == TileType.Save)
                {
                    AddSaveMarker(root, pos, saveMaterial);
                }
            }
        }

        AddSanctuaryWallLamps(root, dungeon, lampMaterial);
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

    private static void AddExitMarker(Node3D root, Vector3 pos, Material material)
    {
        AddDoorPart(root, material, new Vector3(0.18f, 2.4f, 0.18f), pos + new Vector3(-0.62f, 1.2f, 0f));
        AddDoorPart(root, material, new Vector3(0.18f, 2.4f, 0.18f), pos + new Vector3(0.62f, 1.2f, 0f));
        AddDoorPart(root, material, new Vector3(1.35f, 0.18f, 0.18f), pos + new Vector3(0f, 2.35f, 0f));
        AddDoorPart(root, material, new Vector3(1.1f, 2.0f, 0.06f), pos + new Vector3(0f, 1.0f, -0.02f));
    }

    private static void AddSaveMarker(Node3D root, Vector3 pos, Material material)
    {
        AddDoorPart(root, material, new Vector3(0.92f, 0.08f, 0.92f), pos + new Vector3(0f, 0.03f, 0f));
        AddDoorPart(root, material, new Vector3(0.26f, 0.22f, 0.26f), pos + new Vector3(0f, 0.12f, 0f));
    }

    private static void AddSanctuaryWallLamps(Node3D root, DungeonData dungeon, Material lampMaterial)
    {
        if (dungeon.SaveRoomIds.Count == 0 && dungeon.SaveTiles.Count > 0)
        {
            foreach (var tile in dungeon.SaveTiles)
            {
                AddWallLamp(root, lampMaterial, tile + new Vector2I(0, -1), new Vector3(0f, 0f, 0.42f));
                AddWallLamp(root, lampMaterial, tile + new Vector2I(0, 1), new Vector3(0f, 0f, -0.42f));
                AddWallLamp(root, lampMaterial, tile + new Vector2I(-1, 0), new Vector3(0.42f, 0f, 0f));
                AddWallLamp(root, lampMaterial, tile + new Vector2I(1, 0), new Vector3(-0.42f, 0f, 0f));
            }

            return;
        }

        var emitted = new HashSet<int>();
        foreach (var roomId in dungeon.SaveRoomIds)
        {
            if (!dungeon.RoomBounds.TryGetValue(roomId, out var room) || !emitted.Add(roomId))
            {
                continue;
            }

            var cx = room.Position.X + (room.Size.X / 2);
            var cy = room.Position.Y + (room.Size.Y / 2);
            AddWallLamp(root, lampMaterial, new Vector2I(cx, room.Position.Y + 1), new Vector3(0f, 0f, 0.42f));
            AddWallLamp(root, lampMaterial, new Vector2I(cx, room.End.Y - 2), new Vector3(0f, 0f, -0.42f));
            AddWallLamp(root, lampMaterial, new Vector2I(room.Position.X + 1, cy), new Vector3(0.42f, 0f, 0f));
            AddWallLamp(root, lampMaterial, new Vector2I(room.End.X - 2, cy), new Vector3(-0.42f, 0f, 0f));
        }
    }

    private static void AddWallLamp(Node3D root, Material lampMaterial, Vector2I grid, Vector3 normalOffset)
    {
        var basePos = DungeonGenerator.GridToWorld(grid.X, grid.Y, TileSize) + new Vector3(0f, 2.2f, 0f) + normalOffset;
        AddDoorPart(root, lampMaterial, new Vector3(0.22f, 0.34f, 0.22f), basePos);
        var light = new OmniLight3D
        {
            Position = basePos + new Vector3(0f, 0.08f, 0f),
            OmniRange = 6.6f,
            LightEnergy = 1.5f,
            LightColor = new Color(0.94f, 0.97f, 1f),
            ShadowEnabled = false,
        };
        root.AddChild(light);
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
