using Godot;

public static class ExploreLightingRig
{
    public static void Ensure(Node3D root)
    {
        if (root.GetNodeOrNull<Node3D>("LightingRig") is not null)
        {
            return;
        }

        var rig = new Node3D { Name = "LightingRig" };
        root.AddChild(rig);

        rig.AddChild(new DirectionalLight3D
        {
            Name = "KeyLight",
            RotationDegrees = new Vector3(-52f, 38f, 0f),
            LightEnergy = 2.2f,
            LightColor = new Color(1f, 0.97f, 0.92f),
            ShadowEnabled = true,
        });

        rig.AddChild(new OmniLight3D
        {
            Name = "FillLight",
            Position = new Vector3(0f, 6f, 0f),
            LightEnergy = 1.8f,
            OmniRange = 80f,
            LightColor = new Color(0.82f, 0.9f, 1f),
        });

        var environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.08f, 0.1f, 0.12f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.74f, 0.82f, 0.9f),
            AmbientLightEnergy = 1.25f,
            AmbientLightSkyContribution = 0.1f,
        };

        rig.AddChild(new WorldEnvironment
        {
            Name = "WorldEnvironment",
            Environment = environment,
        });
    }
}
