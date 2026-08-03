using System;
using System.Collections.Generic;
using System.Linq;

public static class BattleRewardService
{
    public static (int Exp, int Soli, int Tokens, int Items) Finalize(GameSession session)
    {
        var state = session.State;
        if (state is null)
        {
            return (0, 0, 0, 0);
        }

        var tokens = (state.BattleKills + 1) / 2;
        state.BattleStealTokens = tokens;
        return (state.BattleLootExp, state.BattleClaimableSoli, tokens, state.BattleLootItems.Count);
    }

    public static void ClearAfterReward(GameSession session)
    {
        var state = session.State;
        if (state is null)
        {
            return;
        }

        state.Enemies.Clear();
        state.EnemyQueue.Clear();
        state.ResetBattleInstance();
        session.PendingMove = null;
        session.HasPendingMoveReplace = false;
        session.PendingReplaceIndex = 0;
    }

    public static List<MoveModel> BuildStealableMovePool(GameSession session)
    {
        var state = session.State;
        if (state is null)
        {
            return new List<MoveModel>();
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pool = new List<MoveModel>();
        foreach (var enemy in state.BattleDefeatedEnemies)
        {
            foreach (var move in enemy.Moves)
            {
                if (move is null || move.IsBasicAttack || !seen.Add(move.Name))
                {
                    continue;
                }

                pool.Add(move);
            }
        }

        return pool;
    }

    public static MoveModel? PickStealableMove(GameSession session)
    {
        var state = session.State;
        if (state is null || state.BattleStealTokens <= 0)
        {
            return null;
        }

        var ownedNames = new HashSet<string>(
            state.Player.Moves.Where(move => move is not null).Select(move => move!.Name),
            StringComparer.OrdinalIgnoreCase);
        var ownedFamilies = new HashSet<string>(TypeSystem.FamiliesInMoves(state.Player.Moves), StringComparer.OrdinalIgnoreCase);
        var candidates = BuildStealableMovePool(session)
            .Where(move => !ownedNames.Contains(move.Name))
            .Where(move => CanLearnMove(move, ownedFamilies))
            .ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[state.Rng.NextInt(0, candidates.Count - 1)];
    }

    public static bool TryAddMoveToPlayer(GameSession session, MoveModel move)
    {
        var state = session.State;
        if (state is null)
        {
            return false;
        }

        while (state.Player.Moves.Count < 5)
        {
            state.Player.Moves.Add(null);
        }

        for (var i = 0; i < 5; i++)
        {
            if (state.Player.Moves[i] is not null)
            {
                continue;
            }

            state.Player.Moves[i] = move;
            state.BattleStealTokens = Math.Max(0, state.BattleStealTokens - 1);
            return true;
        }

        session.PendingMove = move;
        session.PendingReplaceIndex = 0;
        session.HasPendingMoveReplace = true;
        return false;
    }

    public static int XpNeededForLevel(int level)
    {
        var mult = 0.5f + 0.2f * Math.Max(0, level - 1);
        return Math.Max(1, (int)MathF.Round(100f * mult));
    }

    private static bool CanLearnMove(MoveModel move, HashSet<string> ownedFamilies)
    {
        var family = TypeSystem.FamilyOfType(TypeSystem.MoveType(move));
        return string.IsNullOrWhiteSpace(family) || ownedFamilies.Contains(family) || ownedFamilies.Count < 2;
    }
}
