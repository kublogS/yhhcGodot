using Godot;
using System.Collections.Generic;

public sealed class RewardFlowCoordinator
{
    private readonly IRewardSessionContext _session;
    private readonly ISaveFlowPersistence _persistence;

    public RewardFlowCoordinator(IRewardSessionContext session, ISaveFlowPersistence persistence)
    {
        _session = session;
        _persistence = persistence;
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
            return new RewardActionResult
            {
                Success = false,
                RequiresMoveReplaceChoice = true,
                Message = $"Move list piena: scegli uno slot da sostituire con {move.Name}.",
            };
        }

        return new RewardActionResult { Success = true, Message = $"Mossa acquisita: {move.Name}." };
    }

    public bool HasPendingMoveReplace()
    {
        return _session.HasPendingMoveReplace && _session.PendingMove is not null;
    }

    public string PendingMoveName()
    {
        return _session.PendingMove?.Name ?? "Mossa";
    }

    public List<string> BuildMoveReplaceSlots()
    {
        var state = _session.State;
        var labels = new List<string>();
        if (state is null)
        {
            return labels;
        }

        while (state.Player.Moves.Count < 5)
        {
            state.Player.Moves.Add(null);
        }

        for (var i = 0; i < 5; i++)
        {
            var move = state.Player.Moves[i];
            labels.Add(move is null ? $"Slot {i + 1}: vuoto" : $"Slot {i + 1}: {move.Name}");
        }

        return labels;
    }

    public RewardActionResult ReplacePendingMoveSlot(int slotIndex)
    {
        var state = _session.State;
        var pendingMove = _session.PendingMove;
        if (state is null || pendingMove is null || !_session.HasPendingMoveReplace)
        {
            return new RewardActionResult { Success = false, Message = "Nessuna sostituzione mossa in attesa." };
        }

        while (state.Player.Moves.Count < 5)
        {
            state.Player.Moves.Add(null);
        }

        var boundedSlot = Mathf.Clamp(slotIndex, 0, 4);
        var replacedName = state.Player.Moves[boundedSlot]?.Name ?? "slot vuoto";
        state.Player.Moves[boundedSlot] = pendingMove;
        state.BattleStealTokens = Mathf.Max(0, state.BattleStealTokens - 1);
        _session.PendingMove = null;
        _session.HasPendingMoveReplace = false;
        _session.PendingReplaceIndex = boundedSlot;
        return new RewardActionResult
        {
            Success = true,
            Message = $"Sostituzione completata: {replacedName} -> {pendingMove.Name} (slot {boundedSlot + 1}).",
        };
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

    public PersistResult FinishAndPersist()
    {
        if (HasPendingMoveReplace())
        {
            return PersistResult.Failed(SceneRoute.Reward, "Seleziona prima lo slot della mossa da sostituire.");
        }

        var checkpoint = GameSessionCheckpoint.Capture(_session, cloneState: true);
        FinalizeRewardCommit();
        if (_persistence.SaveToSlot(_session.CurrentSlot))
        {
            return PersistResult.Succeeded(SceneRoute.Explore, message: "Reward salvato.");
        }

        checkpoint.Restore(_session);
        return PersistResult.Failed(
            SceneRoute.Reward,
            "Salvataggio fallito: reward non confermato, stato ripristinato.",
            recoverable: true);
    }

    private void FinalizeRewardCommit()
    {
        ApplyLevelUps();
        _session.ClearAfterReward();
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
