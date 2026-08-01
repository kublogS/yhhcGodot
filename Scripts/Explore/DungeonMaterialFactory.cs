using Godot;

public static class DungeonMaterialFactory
{
    public static Material CreateDungeonWallMaterial()
    {
        return CreateFlatMaterial(new Color(0.36f, 0.37f, 0.39f, 1f), roughness: 0.96f);
    }

    public static Material CreateHubWallMaterial()
    {
        return CreateFlatMaterial(new Color(0.73f, 0.71f, 0.67f, 1f), roughness: 0.94f);
    }

    public static Material CreateBreakableWallMaterial()
    {
        return CreateFlatMaterial(new Color(0.60f, 0.53f, 0.43f, 1f), roughness: 0.98f);
    }

    public static StandardMaterial3D CreateObsidianMaterial() => CreateObsidianFrameMaterial();

    public static StandardMaterial3D CreateObsidianFrameMaterial()
    {
        return new StandardMaterial3D
        {
            AlbedoColor = new Color(0.05f, 0.05f, 0.08f, 1f),
            Roughness = 0.16f,
            Metallic = 0.12f,
            EmissionEnabled = true,
            Emission = new Color(0.08f, 0.09f, 0.14f, 1f),
            EmissionEnergyMultiplier = 0.08f,
        };
    }

    public static StandardMaterial3D CreateObsidianInsetMaterial()
    {
        return new StandardMaterial3D
        {
            AlbedoColor = new Color(0.08f, 0.08f, 0.13f, 1f),
            Roughness = 0.06f,
            Metallic = 0.22f,
            ClearcoatEnabled = true,
            Clearcoat = 0.55f,
            ClearcoatRoughness = 0.14f,
            EmissionEnabled = true,
            Emission = new Color(0.12f, 0.18f, 0.28f, 1f),
            EmissionEnergyMultiplier = 0.12f,
        };
    }

    public static StandardMaterial3D CreatePortalEdgeMaterial()
    {
        return new StandardMaterial3D
        {
            AlbedoColor = new Color(0.22f, 0.28f, 0.36f, 1f),
            Roughness = 0.18f,
            Metallic = 0.05f,
            EmissionEnabled = true,
            Emission = new Color(0.42f, 0.63f, 0.82f, 1f),
            EmissionEnergyMultiplier = 0.32f,
        };
    }

    private static StandardMaterial3D CreateFlatMaterial(Color color, float roughness, float metallic = 0f)
    {
        return new StandardMaterial3D
        {
            AlbedoColor = color,
            Roughness = roughness,
            Metallic = metallic,
        };
    }
}
