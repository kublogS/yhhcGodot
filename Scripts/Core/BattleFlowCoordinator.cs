using System.Collections.Generic;

public sealed class BattleFlowCoordinator
{
    private readonly GameSession _session;

    public BattleFlowCoordinator(GameSession session)
    {
        _session = session;
    }

    public BattleFlowResult ExecuteTurn(CombatTurnRequest request)
    {
        var result = new BattleFlowResult();
        var state = _session.State;
        if (state is null)
        {
            result.Route = SceneRoute.MainMenu;
            return result;
        }

        var outcome = CombatService.CombatTurn(state, request);
        result.LogLines.AddRange(outcome.LogLines);

        if (outcome.PlayerDefeated)
        {
            result.LogLines.Add("Game Over.");
            result.Route = SceneRoute.MainMenu;
            return result;
        }

        if (outcome.Fled)
        {
            result.Route = SceneRoute.Explore;
            return result;
        }

        if (outcome.BattleEnded)
        {
            var rewards = _session.FinalizeBattleRewards();
            result.LogLines.Add($"Ricompense: Soli {rewards.Soli}, Oggetti {rewards.Items}, Mosse {rewards.Tokens}");
            result.Route = SceneRoute.Reward;
            return result;
        }

        return result;
    }

    public BattleScreenState? BuildScreenState(int selectedTarget)
    {
        var state = _session.State;
        if (state is null)
        {
            return null;
        }

        while (state.Player.Moves.Count < 5)
        {
            state.Player.Moves.Add(null);
        }

        var moves = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            var move = state.Player.Moves[i] ?? Moves.BasicAttackMove();
            moves.Add($"{i + 1}. {move.Name}");
        }

        var targets = new List<BattleTargetView>();
        for (var i = 0; i < 4; i++)
        {
            if (i < state.Enemies.Count)
            {
                var enemy = state.Enemies[i];
                targets.Add(new BattleTargetView
                {
                    Visible = true,
                    Selected = i == selectedTarget,
                    Label = $"{enemy.Name} {enemy.Hp}/{enemy.MaxHp}",
                });
                continue;
            }

            targets.Add(new BattleTargetView { Visible = false, Selected = false, Label = string.Empty });
        }

        return new BattleScreenState { MoveLabels = moves, Targets = targets };
    }
}
