using Godot;

public partial class RewardController : Control
{
    private RichTextLabel _log = null!;
    private RewardFlowCoordinator _flow = null!;

    public override void _Ready()
    {
        _flow = new RewardFlowCoordinator(GameSession.Instance, SaveService.Instance);
        _log = GetNode<RichTextLabel>("Root/Log");
        GetNode<Button>("Root/Buttons/Move").Pressed += () => AppendAction(_flow.ClaimMove());
        GetNode<Button>("Root/Buttons/Money").Pressed += () => AppendAction(_flow.ClaimMoney());
        GetNode<Button>("Root/Buttons/Items").Pressed += () => AppendAction(_flow.ClaimItems());
        GetNode<Button>("Root/Buttons/Continue").Pressed += () => SceneRouteNavigator.Navigate(_flow.FinishAndPersist(), GetTree());
        RefreshSummary();
    }

    private void RefreshSummary()
    {
        if (!_flow.HasActiveState())
        {
            SceneRouteNavigator.Navigate(SceneRoute.MainMenu, GetTree());
            return;
        }

        var summary = _flow.BuildSummary();
        _log.Text =
            $"Battaglia conclusa\n" +
            $"EXP gia assegnata: {summary.Exp}\n" +
            $"Soli disponibili: {summary.Soli}\n" +
            $"Oggetti disponibili: {summary.Items}\n" +
            $"Mosse rubabili: {summary.Tokens}\n";
    }

    private void AppendAction(RewardActionResult action)
    {
        _log.Text += action.Message + "\n";
    }
}
