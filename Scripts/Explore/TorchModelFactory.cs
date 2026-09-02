using Godot;

// Procedural low-poly torch, rebuilt after researching the reference: a
// plain wooden branch carried and lit as a torch, the style of the stick
// Ico carries in the PS2 game "Ico". Ico is also known as one of the first
// PS2-era games to use bloom lighting, which is why the flame here leans on
// additive-blended soft glow billboards (see BuildEmberParticles) and the
// scene's WorldEnvironment glow (see ExploreLightingRig) instead of sharp
// spark quads - the halo-like light stacking is the point, not literal
// embers. The branch itself is a single gently tapered hexagonal prism with
// flat per-face normals (no smoothing), matching the chunky low-poly look
// of the era rather than a crafted/carved handle.
public static class TorchModelFactory
{
    private const int Sides = 6;

    public static Node3D Create()
    {
        var root = new Node3D { Name = "TorchModel" };

        AddPrismSegment(root, yBottom: 0f, yTop: 0.95f, radiusBottom: 0.022f, radiusTop: 0.013f,
            color: new Color(0.33f, 0.26f, 0.18f));

        var tip = new Vector3(0f, 0.95f, 0f);
        AddCone(root, yBase: 0.95f, height: 0.14f, radius: 0.045f,
            color: new Color(0.95f, 0.42f, 0.12f));
        AddCone(root, yBase: 1.02f, height: 0.10f, radius: 0.024f,
            color: new Color(1f, 0.84f, 0.4f));

        root.AddChild(BuildEmberParticles(tip));

        return root;
    }

    private static GpuParticles3D BuildEmberParticles(Vector3 origin)
    {
        var quad = new QuadMesh { Size = new Vector2(0.09f, 0.09f) };
        var material = new StandardMaterial3D
        {
            AlbedoTexture = BuildRadialGlowTexture(),
            AlbedoColor = Colors.White,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BlendMode = BaseMaterial3D.BlendModeEnum.Add,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            VertexColorUseAsAlbedo = true,
        };
        quad.SurfaceSetMaterial(0, material);

        var fade = new Gradient();
        fade.SetColor(0, new Color(1f, 0.85f, 0.5f, 0.9f));
        fade.SetColor(1, new Color(1f, 0.5f, 0.15f, 0f));

        var process = new ParticleProcessMaterial
        {
            Direction = new Vector3(0f, 1f, 0f),
            Spread = 22f,
            InitialVelocityMin = 0.1f,
            InitialVelocityMax = 0.26f,
            Gravity = new Vector3(0f, 0.18f, 0f),
            ScaleMin = 0.3f,
            ScaleMax = 0.9f,
            ColorRamp = new GradientTexture1D { Gradient = fade },
        };

        return new GpuParticles3D
        {
            Name = "EmberGlowParticles",
            Position = origin + new Vector3(0f, 0.05f, 0f),
            DrawPass1 = quad,
            ProcessMaterial = process,
            Amount = 10,
            Lifetime = 1.5,
        };
    }

    private static GradientTexture2D BuildRadialGlowTexture()
    {
        var gradient = new Gradient();
        gradient.SetColor(0, new Color(1f, 1f, 1f, 1f));
        gradient.SetColor(1, new Color(1f, 1f, 1f, 0f));

        return new GradientTexture2D
        {
            Gradient = gradient,
            Width = 32,
            Height = 32,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f),
            FillTo = new Vector2(1f, 0.5f),
        };
    }

    private static void AddPrismSegment(Node3D root, float yBottom, float yTop, float radiusBottom, float radiusTop, Color color)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        for (var i = 0; i < Sides; i++)
        {
            var a0 = i / (float)Sides * Mathf.Tau;
            var a1 = (i + 1) / (float)Sides * Mathf.Tau;
            var p0 = new Vector3(Mathf.Cos(a0) * radiusBottom, yBottom, Mathf.Sin(a0) * radiusBottom);
            var p1 = new Vector3(Mathf.Cos(a1) * radiusBottom, yBottom, Mathf.Sin(a1) * radiusBottom);
            var p2 = new Vector3(Mathf.Cos(a1) * radiusTop, yTop, Mathf.Sin(a1) * radiusTop);
            var p3 = new Vector3(Mathf.Cos(a0) * radiusTop, yTop, Mathf.Sin(a0) * radiusTop);
            var normal = (p1 - p0).Cross(p3 - p0).Normalized();

            AddTri(st, color, normal, p0, p1, p2);
            AddTri(st, color, normal, p0, p2, p3);
        }

        st.Index();
        var mesh = st.Commit();
        root.AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = BuildMaterial(color) });
    }

    private static void AddCone(Node3D root, float yBase, float height, float radius, Color color)
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        var apex = new Vector3(0f, yBase + height, 0f);
        for (var i = 0; i < Sides; i++)
        {
            var a0 = i / (float)Sides * Mathf.Tau;
            var a1 = (i + 1) / (float)Sides * Mathf.Tau;
            var p0 = new Vector3(Mathf.Cos(a0) * radius, yBase, Mathf.Sin(a0) * radius);
            var p1 = new Vector3(Mathf.Cos(a1) * radius, yBase, Mathf.Sin(a1) * radius);
            var normal = (p1 - p0).Cross(apex - p0).Normalized();

            AddTri(st, color, normal, p0, p1, apex);
        }

        st.Index();
        var mesh = st.Commit();
        var material = BuildMaterial(color);
        material.EmissionEnabled = true;
        material.Emission = color;
        material.EmissionEnergyMultiplier = 1.6f;
        root.AddChild(new MeshInstance3D { Mesh = mesh, MaterialOverride = material });
    }

    private static void AddTri(SurfaceTool st, Color color, Vector3 normal, Vector3 a, Vector3 b, Vector3 c)
    {
        st.SetColor(color);
        st.SetNormal(normal);
        st.AddVertex(a);
        st.AddVertex(b);
        st.AddVertex(c);
    }

    private static StandardMaterial3D BuildMaterial(Color color)
    {
        return new StandardMaterial3D
        {
            AlbedoColor = color,
            Roughness = 0.85f,
            Metallic = 0f,
            // Winding direction isn't hand-verified per face here, so disable
            // culling rather than risk invisible backwards-wound triangles.
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };
    }
}
