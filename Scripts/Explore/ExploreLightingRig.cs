using Godot;

public static class ExploreLightingRig
{
    private const string RigPath = "LightingRig";
    private const string KeyLightPath = $"{RigPath}/KeyLight";
    private const string FillLightPath = $"{RigPath}/FillLight";
    private const string WorldEnvPath = $"{RigPath}/WorldEnvironment";

    public static void Ensure(Node3D root)
    {
        if (root.GetNodeOrNull<Node3D>(RigPath) is not null)
        {
            return;
        }

        var rig = new Node3D { Name = RigPath };
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

    public static void SetDungeonMood(Node3D root, bool dungeonMode)
    {
        Ensure(root);
        var key = root.GetNodeOrNull<DirectionalLight3D>(KeyLightPath);
        var fill = root.GetNodeOrNull<OmniLight3D>(FillLightPath);
        var world = root.GetNodeOrNull<WorldEnvironment>(WorldEnvPath);
        var environment = world?.Environment;

        if (dungeonMode)
        {
            if (key is not null)
            {
                key.Visible = false;
                key.LightEnergy = 0f;
            }

            if (fill is not null)
            {
                fill.Visible = true;
                fill.LightEnergy = 0.22f;
                fill.OmniRange = 26f;
                fill.LightColor = new Color(0.26f, 0.31f, 0.36f);
            }

            if (environment is not null)
            {
                environment.BackgroundColor = new Color(0.03f, 0.035f, 0.045f);
                environment.AmbientLightColor = new Color(0.18f, 0.2f, 0.24f);
                environment.AmbientLightEnergy = 0.24f;
                environment.AmbientLightSkyContribution = 0f;
            }

            return;
        }

        if (key is not null)
        {
            key.Visible = true;
            key.LightEnergy = 1.35f;
        }

        if (fill is not null)
        {
            fill.Visible = true;
            fill.LightEnergy = 1.05f;
            fill.OmniRange = 80f;
            fill.LightColor = new Color(0.82f, 0.9f, 1f);
        }

        if (environment is not null)
        {
            environment.BackgroundColor = new Color(0.08f, 0.1f, 0.12f);
            environment.AmbientLightColor = new Color(0.74f, 0.82f, 0.9f);
            environment.AmbientLightEnergy = 1.0f;
            environment.AmbientLightSkyContribution = 0.1f;
        }
    }
}
