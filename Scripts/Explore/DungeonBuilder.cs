using System.Collections.Generic;
using Godot;

public static class DungeonBuilder
{
    private const float WallHeight = 6.6f;
    private const float WallColliderInset = 0.24f;
    private const string BreakableNodePrefix = "BreakableTile_";

    public static void Build(Node3D root, DungeonData dungeon)
    {
        Clear(root);
        var tileSize = DungeonMetrics.TileSize;
        var floorMesh = new BoxMesh { Size = new Vector3(tileSize, 0.1f, tileSize) };
        var wallMesh = new BoxMesh { Size = new Vector3(tileSize, WallHeight, tileSize) };
        var wallShape = new BoxShape3D
        {
            Size = new Vector3(tileSize - (WallColliderInset * 2f), WallHeight, tileSize - (WallColliderInset * 2f)),
        };

        var floorMaterial = MakeMaterial(PythonColorPalette.FloorLightGray);
        var wallMaterial = DungeonMaterialFactory.CreateDungeonWallMaterial();
        var breakableMaterial = DungeonMaterialFactory.CreateBreakableWallMaterial();
        var saveMaterial = MakeMaterial(new Color(0.84f, 0.9f, 1f, 1f));
        var lampMaterial = MakeMaterial(new Color(0.72f, 0.75f, 0.8f, 1f));
        var exitMaterial = DungeonMaterialFactory.CreateObsidianMaterial();

        for (var y = 0; y < dungeon.Height; y++)
        {
            for (var x = 0; x < dungeon.Width; x++)
            {
                var tile = dungeon.GetTile(x, y);
                var state = dungeon.GetTraversalState(x, y);
                var pos = DungeonGenerator.GridToWorld(x, y, tileSize);
                if (state != TileTraversalState.Solid)
                {
                    AddMesh(root, floorMesh, floorMaterial, pos + new Vector3(0f, -0.05f, 0f));
                }

                if (state == TileTraversalState.BreakableIntact)
                {
                    var tilePos = new Vector2I(x, y);
                    AddWall(root, wallMesh, breakableMaterial, wallShape, pos + new Vector3(0f, WallHeight * 0.5f - 0.1f, 0f), BreakableNodeName(tilePos));
                    continue;
                }

                if (tile == TileType.Wall || state == TileTraversalState.Solid)
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

    public static bool TryRemoveBreakableNode(Node3D root, Vector2I tile)
    {
        var candidate = root.FindChild(BreakableNodeName(tile), recursive: true, owned: false) as Node;
        if (candidate is null)
        {
            return false;
        }

        candidate.QueueFree();
        return true;
    }

    private static string BreakableNodeName(Vector2I tile)
    {
        return $"{BreakableNodePrefix}{tile.X}_{tile.Y}";
    }

    private static StandardMaterial3D MakeMaterial(Color color)
    {
        return new StandardMaterial3D { AlbedoColor = color, Roughness = 1.0f, Metallic = 0f };
    }

    private static void AddMesh(Node3D root, Mesh mesh, Material material, Vector3 pos)
    {
        root.AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Position = pos });
    }

    private static void AddWall(Node3D root, Mesh mesh, Material material, Shape3D shape, Vector3 pos, string? nodeName = null)
    {
        var wall = new Node3D { Position = pos };
        if (!string.IsNullOrWhiteSpace(nodeName))
        {
            wall.Name = nodeName;
        }

        wall.AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = material });
        var body = new StaticBody3D();
        body.AddChild(new CollisionShape3D { Shape = shape });
        wall.AddChild(body);
        root.AddChild(wall);
    }

    private static void AddExitMarker(Node3D root, Vector3 pos, Material material)
    {
        AddDoorPart(root, material, new Vector3(0.44f, 4.8f, 0.34f), pos + new Vector3(-1.24f, 2.4f, 0.08f));
        AddDoorPart(root, material, new Vector3(0.44f, 4.8f, 0.34f), pos + new Vector3(1.24f, 2.4f, 0.08f));
        AddDoorPart(root, material, new Vector3(2.84f, 0.44f, 0.34f), pos + new Vector3(0f, 4.68f, 0.08f));
        AddExitPortalPanel(root, pos + new Vector3(0f, 1.98f, -0.06f), new Vector2(2.16f, 4.04f));
    }

    private static void AddExitPortalPanel(Node3D root, Vector3 position, Vector2 size)
    {
        var panel = PortalVisualFactory.Create(size);
        panel.Position = position;
        root.AddChild(panel);
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
        var basePos = DungeonGenerator.GridToWorld(grid.X, grid.Y, DungeonMetrics.TileSize) + new Vector3(0f, 2.2f, 0f) + normalOffset;
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
