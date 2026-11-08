using System;
using System.Threading.Tasks;
using Godot;

public static class BattleTurnPlayback
{
    private const float EventDelaySeconds = 0.24f;

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
            if (!IsAlive(host))
            {
                // The turn is already resolved in GameState; only the
                // presentation is cut short (scene change / overlay freed).
                break;
            }

            if (eventPresenter is not null)
            {
                await eventPresenter(battleEvent);
                if (!IsAlive(host))
                {
                    break;
                }
            }

            viewState.AppendLog(battleEvent.Message);
            await host.ToSignal(host.GetTree().CreateTimer(EventDelaySeconds), SceneTreeTimer.SignalName.Timeout);
        }

        return result;
    }

    public static bool IsAlive(Node host)
    {
        return GodotObject.IsInstanceValid(host) && host.IsInsideTree();
    }
}
