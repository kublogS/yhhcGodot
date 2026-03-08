using Godot;

public sealed class RewardFlowCoordinator
{
    private readonly GameSession _session;
    private readonly SaveService _saveService;

    public RewardFlowCoordinator(GameSession session, SaveService saveService)
    {
        _session = session;
        _saveService = saveService;
    }

    public bool HasActiveState()
    {
        return _session.State is not null;
    }

    public RewardSummary BuildSummary()
    {
        var rewards = _session.FinalizeBattleRewards();
        return new RewardSummary
        {
            Exp = rewards.Exp,
            Soli = rewards.Soli,
            Items = rewards.Items,
            Tokens = rewards.Tokens,
        };
    }

    public RewardActionResult ClaimMove()
    {
        var state = _session.State;
        if (state is null)
        {
            return new RewardActionResult { Success = false, Message = "Stato partita non valido." };
        }

        var move = _session.PickStealableMove();
        if (move is null)
        {
            return new RewardActionResult { Success = false, Message = "Nessuna mossa ottenibile." };
        }

        if (!_session.TryAddMoveToPlayer(move))
        {
            state.Player.Moves[0] = move;
            state.BattleStealTokens = Mathf.Max(0, state.BattleStealTokens - 1);
            return new RewardActionResult { Success = true, Message = $"Move list piena: sostituito slot 1 con {move.Name}." };
        }

        return new RewardActionResult { Success = true, Message = $"Mossa acquisita: {move.Name}." };
    }

    public RewardActionResult ClaimMoney()
    {
        var state = _session.State;
        if (state is null)
        {
            return new RewardActionResult { Success = false, Message = "Stato partita non valido." };
        }

        var gained = Inventory.ClaimMoney(state, state.Player);
        var text = gained > 0 ? $"Ottenuti {gained} soli." : "Nessun soli disponibile.";
        return new RewardActionResult { Success = gained > 0, Message = text };
    }

    public RewardActionResult ClaimItems()
    {
        var state = _session.State;
        if (state is null)
        {
            return new RewardActionResult { Success = false, Message = "Stato partita non valido." };
        }

        var claimed = Inventory.ClaimItems(state, state.Player);
        var text = claimed.Count > 0 ? $"Oggetti: {string.Join(", ", claimed)}" : "Nessun oggetto disponibile.";
        return new RewardActionResult { Success = claimed.Count > 0, Message = text };
    }

    public SceneRoute FinishAndPersist()
    {
        ApplyLevelUps();
        _session.ClearAfterReward();
        _saveService.SaveToSlot(_session.CurrentSlot);
        return SceneRoute.Explore;
    }

    private void ApplyLevelUps()
    {
        var state = _session.State;
        if (state is null)
        {
            return;
        }

        while (state.Player.Exp >= _session.XpNeededForLevel(state.Player.Level))
        {
            state.Player.Exp -= _session.XpNeededForLevel(state.Player.Level);
            state.Player.Level += 1;
            state.Player.MaxHp += 1;
            state.Player.Hp = Mathf.Min(state.Player.MaxHp, state.Player.Hp + 1);
            state.Player.Forza += 1;
            state.Player.Magia += 1;
            state.Player.Difesa += 1;
            state.Player.Agilita += 1;
            state.Player.Fortuna += 1;
            state.Player.StatPoints += 2;
        }
    }
}
