using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameSession
{
    public (int Exp, int Soli, int Tokens, int Items) FinalizeBattleRewards()
    {
        if (State is null)
        {
            return (0, 0, 0, 0);
        }

        var tokens = (State.BattleKills + 1) / 2;
        State.BattleStealTokens = tokens;
        return (State.BattleLootExp, State.BattleClaimableSoli, tokens, State.BattleLootItems.Count);
    }

    public void ClearAfterReward()
    {
        if (State is null)
        {
            return;
        }

        State.Enemies.Clear();
        State.EnemyQueue.Clear();
        State.SyncEnemyLegacy();
        State.ResetBattleInstance();
        PendingMove = null;
        HasPendingMoveReplace = false;
        PendingReplaceIndex = 0;
    }

    public List<MoveModel> StealableMovePool()
    {
        if (State is null)
        {
            return new List<MoveModel>();
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pool = new List<MoveModel>();
        foreach (var enemy in State.BattleDefeatedEnemies)
        {
            foreach (var move in enemy.Moves)
            {
                if (move is null || move.IsBasicAttack)
                {
                    continue;
                }

                if (seen.Add(move.Name))
                {
                    pool.Add(move);
                }
            }
        }

        return pool;
    }

    public MoveModel? PickStealableMove()
    {
        if (State is null || State.BattleStealTokens <= 0)
        {
            return null;
        }

        var pool = StealableMovePool();
        if (pool.Count == 0)
        {
            return null;
        }

        var ownedNames = new HashSet<string>(State.Player.Moves.Where(m => m is not null).Select(m => m!.Name), StringComparer.OrdinalIgnoreCase);
        var ownedFamilies = new HashSet<string>(TypeSystem.FamiliesInMoves(State.Player.Moves), StringComparer.OrdinalIgnoreCase);
        var candidates = new List<MoveModel>();
        foreach (var move in pool)
        {
            if (ownedNames.Contains(move.Name))
            {
                continue;
            }

            var fam = TypeSystem.FamilyOfType(TypeSystem.MoveType(move));
            if (!string.IsNullOrWhiteSpace(fam) && !ownedFamilies.Contains(fam!) && ownedFamilies.Count >= 2)
            {
                continue;
            }

            candidates.Add(move);
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[State.Rng.NextInt(0, candidates.Count - 1)];
    }

    public bool TryAddMoveToPlayer(MoveModel move)
    {
        if (State is null)
        {
            return false;
        }

        while (State.Player.Moves.Count < 5)
        {
            State.Player.Moves.Add(null);
        }

        for (var i = 0; i < 5; i++)
        {
            if (State.Player.Moves[i] is null)
            {
                State.Player.Moves[i] = move;
                State.BattleStealTokens = Math.Max(0, State.BattleStealTokens - 1);
                return true;
            }
        }

        PendingMove = move;
        PendingReplaceIndex = 0;
        HasPendingMoveReplace = true;
        return false;
    }

    public int XpNeededForLevel(int level)
    {
        var mult = 0.5f + 0.2f * Math.Max(0, level - 1);
        return Math.Max(1, (int)MathF.Round(100f * mult));
    }
}
