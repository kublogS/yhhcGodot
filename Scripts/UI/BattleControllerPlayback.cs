using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BattleController
{
    private const float EventPlaybackDelaySeconds = 0.5f;
    private bool _playbackLocked;

    private bool IsPlaybackLocked()
    {
        return _playbackLocked;
    }

    private void SetPlaybackLock(bool locked)
    {
        _playbackLocked = locked;
        if (locked)
        {
            foreach (var button in AllInteractiveButtons())
            {
                button.Disabled = true;
            }

            return;
        }

        foreach (var button in _moveButtons)
        {
            button.Disabled = false;
        }

        foreach (var button in _actionButtons)
        {
            button.Disabled = false;
        }

        foreach (var button in _targetButtons)
        {
            button.Disabled = !button.Visible;
        }

        RefreshFocusAfterUiUpdate();
    }

    private async Task PlayEventSequence(IReadOnlyList<CombatEventEntry> events, IReadOnlyList<string> fallbackLines)
    {
        var stream = events.Count > 0 ? events : BuildFallbackEvents(fallbackLines);
        if (stream.Count == 0)
        {
            return;
        }

        foreach (var battleEvent in stream)
        {
            AppendLog(battleEvent.Message);
            TriggerDamageFeedback(battleEvent);
            await ToSignal(GetTree().CreateTimer(EventPlaybackDelaySeconds), SceneTreeTimer.SignalName.Timeout);
        }
    }

    private static List<CombatEventEntry> BuildFallbackEvents(IReadOnlyList<string> lines)
    {
        var fallback = new List<CombatEventEntry>(lines.Count);
        foreach (var line in lines)
        {
            fallback.Add(new CombatEventEntry
            {
                EventType = CombatEventType.SystemMessage,
                Message = line,
            });
        }

        return fallback;
    }
}
