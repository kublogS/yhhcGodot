using System.Threading.Tasks;
using Godot;

public static class InWorldBattlePresentationPlayback
{
    public static async Task DashAsync(Node host, InWorldBattleParticipant source, InWorldBattleParticipant? target, Vector3 battleCenter)
    {
        var targetPoint = target?.Root.GlobalPosition ?? battleCenter;
        source.FacePoint(targetPoint);
        var dashPoint = source.AnchorPosition.Lerp(targetPoint, 0.28f);
        var tween = host.CreateTween();
        tween.TweenProperty(source.Root, "global_position", dashPoint, 0.11f).SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(source.Root, "global_position", source.AnchorPosition, 0.14f).SetTrans(Tween.TransitionType.Sine);
        await host.ToSignal(tween, Tween.SignalName.Finished);
    }

    public static async Task ImpactAsync(Node host, InWorldBattleParticipant target, int? amount)
    {
        var recoil = target.AnchorPosition - (target.Root.GlobalTransform.Basis.Z * 0.16f);
        var scaleUp = target.Root.Scale * 1.05f;
        var tween = host.CreateTween();
        tween.TweenProperty(target.Root, "global_position", recoil, 0.06f).SetTrans(Tween.TransitionType.Quad);
        tween.Parallel().TweenProperty(target.Root, "scale", scaleUp, 0.06f);
        tween.TweenProperty(target.Root, "global_position", target.AnchorPosition, 0.1f).SetTrans(Tween.TransitionType.Sine);
        tween.Parallel().TweenProperty(target.Root, "scale", Vector3.One, 0.1f);
        if (amount.GetValueOrDefault() > 0)
        {
            SpawnFloatingText(host, target.Root.GlobalPosition + new Vector3(0f, 1.6f, 0f), amount!.Value.ToString(), new Color(1f, 0.55f, 0.55f));
        }

        await host.ToSignal(tween, Tween.SignalName.Finished);
    }

    public static async Task DefendPulseAsync(Node host, InWorldBattleParticipant source)
    {
        var tween = host.CreateTween();
        tween.TweenProperty(source.Root, "scale", new Vector3(1.08f, 1.08f, 1.08f), 0.08f);
        tween.TweenProperty(source.Root, "scale", Vector3.One, 0.12f);
        await host.ToSignal(tween, Tween.SignalName.Finished);
    }

    public static async Task FleeAsync(Node host, InWorldBattleParticipant source)
    {
        var retreat = source.AnchorPosition - (source.Root.GlobalTransform.Basis.Z * 0.8f);
        var tween = host.CreateTween();
        tween.TweenProperty(source.Root, "global_position", retreat, 0.18f).SetTrans(Tween.TransitionType.Cubic);
        tween.Parallel().TweenProperty(source.Root, "scale", new Vector3(0.88f, 0.88f, 0.88f), 0.18f);
        await host.ToSignal(tween, Tween.SignalName.Finished);
    }

    public static async Task DefeatAsync(Node host, InWorldBattleParticipant target)
    {
        var fallen = target.Root.GlobalPosition - new Vector3(0f, 0.55f, 0f);
        var tween = host.CreateTween();
        tween.TweenProperty(target.Root, "global_position", fallen, 0.18f).SetTrans(Tween.TransitionType.Cubic);
        tween.Parallel().TweenProperty(target.Root, "scale", new Vector3(0.15f, 0.15f, 0.15f), 0.18f);
        await host.ToSignal(tween, Tween.SignalName.Finished);
        target.SetVisible(false);
    }

    private static void SpawnFloatingText(Node host, Vector3 origin, string text, Color color)
    {
        var label = new Label3D
        {
            Text = text,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            Modulate = color,
            FontSize = 32,
            Position = origin,
        };
        host.AddChild(label);
        var tween = host.CreateTween();
        tween.TweenProperty(label, "position:y", origin.Y + 0.55f, 0.42f);
        tween.Parallel().TweenProperty(label, "modulate:a", 0.0f, 0.42f);
        tween.Finished += label.QueueFree;
    }
}
