using System.Collections.Generic;
using Godot;

public static class ManualPosterBuilder
{
    private static readonly Vector3[] SheetPositions =
    {
        new(-0.42f, 0.2f, 0f),
        new(0.12f, 0.1f, 0.01f),
        new(0.0f, -0.32f, -0.01f),
    };

    private static readonly Vector2[] SheetSizes =
    {
        new(0.66f, 0.84f),
        new(0.64f, 0.82f),
        new(0.74f, 0.9f),
    };

    private static readonly float[] SheetTilts = { 0.03f, -0.02f, 0.01f };

    public static Node3D Build(Node3D parent, Vector3 anchorPosition, Vector3 wallNormal)
    {
        var normal = wallNormal.LengthSquared() < 0.001f ? Vector3.Forward : wallNormal.Normalized();
        var root = new Node3D
        {
            Name = "ManualPanel",
            Position = anchorPosition,
            Rotation = new Vector3(0f, Mathf.Atan2(normal.X, normal.Z), 0f),
        };
        parent.AddChild(root);

        var pages = ManualTextRepository.LoadZonePages("INDICE");
        for (var i = 0; i < SheetPositions.Length; i++)
        {
            var page = i < pages.Count ? pages[i] : ("Manuale", "Apri con X o M per leggere tutte le pagine.");
            AddSheet(root, SheetPositions[i], SheetSizes[i], SheetTilts[i], Shade(PythonColorPalette.ManualSheet, 0.9f + (i * 0.05f)));
            AddSheetText(root, SheetPositions[i], SheetSizes[i], SheetTilts[i], page.Item1, ManualTextRepository.BuildExcerpt(page.Item2, 5, 28));
        }

        return root;
    }

    private static void AddSheet(Node3D root, Vector3 localPosition, Vector2 size, float tilt, Color color)
    {
        var material = new StandardMaterial3D
        {
            AlbedoColor = color,
            Roughness = 0.95f,
            Metallic = 0f,
        };

        root.AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(size.X, size.Y, 0.025f) },
            MaterialOverride = material,
            Position = localPosition,
            Rotation = new Vector3(tilt, 0f, 0f),
        });
    }

    private static void AddSheetText(Node3D root, Vector3 localPosition, Vector2 size, float tilt, string title, string excerpt)
    {
        var label = new Label3D
        {
            Text = title + "\n\n" + excerpt,
            FontSize = 18,
            Modulate = PythonColorPalette.GrayDark,
            OutlineSize = 1,
            OutlineModulate = new Color(1f, 1f, 1f, 0.25f),
            PixelSize = 0.0037f,
            Position = localPosition + new Vector3(-(size.X * 0.43f), size.Y * 0.38f, 0.018f),
            Rotation = new Vector3(tilt, 0f, 0f),
            Width = size.X * 310f,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        root.AddChild(label);
    }

    private static Color Shade(Color color, float factor)
    {
        return new Color(
            Mathf.Clamp(color.R * factor, 0f, 1f),
            Mathf.Clamp(color.G * factor, 0f, 1f),
            Mathf.Clamp(color.B * factor, 0f, 1f),
            1f);
    }
}
