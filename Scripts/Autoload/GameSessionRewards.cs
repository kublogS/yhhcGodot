using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameSession
{
    public (int Exp, int Soli, int Tokens, int Items) FinalizeBattleRewards()
    {
        return BattleRewardService.Finalize(this);
    }

    public void ClearAfterReward()
    {
        BattleRewardService.ClearAfterReward(this);
    }

    public List<MoveModel> StealableMovePool()
    {
        return BattleRewardService.BuildStealableMovePool(this);
    }

    public MoveModel? PickStealableMove()
    {
        return BattleRewardService.PickStealableMove(this);
    }

    public bool TryAddMoveToPlayer(MoveModel move)
    {
        return BattleRewardService.TryAddMoveToPlayer(this, move);
    }

    public int XpNeededForLevel(int level)
    {
        return BattleRewardService.XpNeededForLevel(level);
    }
}
