using System;
using System.Threading.Tasks;
using Godot;

public static class BattleTurnPlayback
{
    public static async Task<BattleFlowResult> ExecuteAsync(
        Node host,
        BattleFlowCoordinator flow,
        CombatTurnRequest request,
        BattleOverlayViewState viewState,
        Func<CombatEventEntry, Task>? eventPresenter)
    {
        var result = flow.ExecuteTurn(request);
        var stream = result.Events.Count > 0 ? result.Events : BattleLogEventFactory.FromLogLines(result.LogLines);
        foreach (var battleEvent in stream)
        {
            if (eventPresenter is not null)
            {
                await eventPresenter(battleEvent);
            }

            viewState.AppendLog(battleEvent.Message);
            await host.ToSignal(host.GetTree().CreateTimer(0.24f), SceneTreeTimer.SignalName.Timeout);
        }

        return result;
    }
}
