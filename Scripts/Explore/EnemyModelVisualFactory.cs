using Godot;

public sealed class EnemyModelVisualFactory
{
    public Node3D? Create(string? spriteId)
    {
        if (!EnemyModelLibrary.TryResolve(spriteId, out var binding))
        {
            return null;
        }

        PackedScene? packed;
        try
        {
            packed = GD.Load<PackedScene>(binding.ScenePath);
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"Caricamento modello nemico fallito per '{spriteId}' da {binding.ScenePath}: {ex.Message}");
            return null;
        }

        if (packed is null)
        {
            return null;
        }

        Node? instance;
        try
        {
            instance = packed.Instantiate();
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"Istanziazione modello nemico fallita per '{spriteId}' da {binding.ScenePath}: {ex.Message}");
            return null;
        }

        if (instance is null)
        {
            return null;
        }

        var modelRoot = instance as Node3D;
        if (modelRoot is null)
        {
            GD.PrintErr($"Il modello nemico '{spriteId}' non produce un Node3D valido: {binding.ScenePath}");
            instance.QueueFree();
            return null;
        }

        var visualRoot = new Node3D
        {
            Name = "VisualRoot",
        };

        var visualPivot = new Node3D
        {
            Name = "VisualPivot",
            Position = new Vector3(0f, binding.HeightOffset, 0f),
            RotationDegrees = new Vector3(0f, binding.YawDegrees, 0f),
        };

        var importedModel = new Node3D
        {
            Name = "ImportedModel",
            Scale = binding.Scale,
        };

        visualRoot.AddChild(visualPivot);
        visualPivot.AddChild(importedModel);
        importedModel.AddChild(modelRoot);
        ConfigureAnimationPlayers(modelRoot, binding.PlayImportedAnimations);
        return visualRoot;
    }

    private static void ConfigureAnimationPlayers(Node root, bool playImportedAnimations)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is Node node)
            {
                ConfigureAnimationPlayers(node, playImportedAnimations);
            }
        }

        if (root is AnimationTree tree)
        {
            tree.Active = playImportedAnimations;
        }

        if (root is not AnimationPlayer player)
        {
            return;
        }

        if (!playImportedAnimations)
        {
            player.Stop();
            player.Autoplay = string.Empty;
            return;
        }

        var names = player.GetAnimationList();
        if (names.Length == 0)
        {
            return;
        }

        var first = names[0];
        var animation = player.GetAnimation(first);
        if (animation is not null && animation.LoopMode == Animation.LoopModeEnum.None)
        {
            animation.LoopMode = Animation.LoopModeEnum.Linear;
        }

        player.Play(first);
    }
}
