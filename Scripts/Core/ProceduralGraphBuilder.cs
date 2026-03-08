using System;
using System.Collections.Generic;
using System.Linq;

public static class ProceduralGraphBuilder
{
    public static ProcRoomGraph Build(int seed, int floorIndex, ProcGraphParams? customParams = null)
    {
        var p = customParams ?? new ProcGraphParams();
        var rng = new Random(seed + (floorIndex * 9973));
        var graph = new ProcRoomGraph { StartId = 0, BossId = p.Depth };

        graph.Nodes[0] = new ProcRoomNode { Id = 0, Type = ProcRoomType.Start, Depth = 0, MainPath = true };
        var prev = 0;
        for (var i = 1; i < p.Depth; i++)
        {
            graph.Nodes[i] = new ProcRoomNode { Id = i, Type = ProcRoomType.Normal, Depth = i, MainPath = true };
            graph.AddEdge(prev, i);
            prev = i;
        }

        graph.Nodes[p.Depth] = new ProcRoomNode { Id = p.Depth, Type = ProcRoomType.Boss, Depth = p.Depth, MainPath = true };
        graph.AddEdge(prev, p.Depth);

        TagMainPathSpecialRooms(graph, rng, p);
        var nextId = AddRandomBranches(graph, rng, p);
        EnsureRoomCountRange(graph, rng, p, ref nextId);
        UpdateDifficultyAndRewards(graph);
        return graph;
    }

    private static void TagMainPathSpecialRooms(ProcRoomGraph graph, Random rng, ProcGraphParams p)
    {
        var shopCandidates = graph.Nodes.Values.Where(n => n.MainPath && n.Depth >= p.ShopDepthMin && n.Type == ProcRoomType.Normal).ToList();
        if (shopCandidates.Count > 0)
        {
            shopCandidates[rng.Next(shopCandidates.Count)].Type = ProcRoomType.Shop;
        }

        var eliteCandidates = graph.Nodes.Values.Where(n => n.MainPath && n.Depth >= p.EliteDepthMin && n.Type == ProcRoomType.Normal && n.Id != graph.BossId).ToList();
        if (eliteCandidates.Count > 0 && rng.NextDouble() < 0.6)
        {
            eliteCandidates[rng.Next(eliteCandidates.Count)].Type = ProcRoomType.Elite;
        }
    }

    private static int AddRandomBranches(ProcRoomGraph graph, Random rng, ProcGraphParams p)
    {
        var nextId = graph.BossId + 1;
        var rewardCooldown = 0;
        var baseNodes = graph.Nodes.Values.Where(n => n.MainPath && n.Id != graph.StartId && n.Id != graph.BossId).Select(n => n.Id).ToList();
        Shuffle(rng, baseNodes);

        foreach (var baseId in baseNodes)
        {
            if (graph.Nodes[baseId].Neighbors.Count >= p.MaxDegree || rng.NextDouble() > p.BranchChance)
            {
                continue;
            }

            var branchLength = rng.Next(p.BranchMin, p.BranchMax + 1);
            var parentId = baseId;
            for (var i = 0; i < branchLength; i++)
            {
                if (graph.Nodes[parentId].Neighbors.Count >= p.MaxDegree)
                {
                    break;
                }

                var type = PickBranchType(graph, rng, p, ref rewardCooldown, baseId);
                graph.Nodes[nextId] = new ProcRoomNode
                {
                    Id = nextId,
                    Type = type,
                    Depth = graph.Nodes[baseId].Depth + 1,
                    MainPath = false,
                };
                graph.AddEdge(parentId, nextId);
                parentId = nextId;
                nextId++;
            }
        }

        return nextId;
    }

    private static ProcRoomType PickBranchType(ProcRoomGraph graph, Random rng, ProcGraphParams p, ref int rewardCooldown, int baseId)
    {
        if (rewardCooldown == 0 && rng.NextDouble() < 0.6)
        {
            rewardCooldown = p.RewardCooldown;
            return ProcRoomType.Reward;
        }

        if (rng.NextDouble() < 0.15 && graph.Nodes[baseId].Depth >= p.EliteDepthMin)
        {
            return ProcRoomType.Elite;
        }

        if (rewardCooldown > 0)
        {
            rewardCooldown--;
        }

        return ProcRoomType.Normal;
    }

    private static void EnsureRoomCountRange(ProcRoomGraph graph, Random rng, ProcGraphParams p, ref int nextId)
    {
        while (graph.Nodes.Count < p.MinRooms)
        {
            var candidates = graph.Nodes.Values.Where(n => n.Id != graph.BossId && n.Neighbors.Count < p.MaxDegree).ToList();
            if (candidates.Count == 0)
            {
                break;
            }

            var parent = candidates[rng.Next(candidates.Count)];
            graph.Nodes[nextId] = new ProcRoomNode { Id = nextId, Type = ProcRoomType.Normal, Depth = parent.Depth + 1, MainPath = false };
            graph.AddEdge(parent.Id, nextId);
            nextId++;
        }

        while (graph.Nodes.Count > p.MaxRooms)
        {
            var removable = graph.Nodes.Values.Where(n => n.Id != graph.StartId && n.Id != graph.BossId && !n.MainPath && n.Neighbors.Count <= 1).ToList();
            if (removable.Count == 0)
            {
                removable = graph.Nodes.Values.Where(n => n.Id != graph.StartId && n.Id != graph.BossId && n.Neighbors.Count <= 1).ToList();
            }

            if (removable.Count == 0)
            {
                break;
            }

            var node = removable[rng.Next(removable.Count)];
            foreach (var neighborId in node.Neighbors)
            {
                graph.Nodes[neighborId].Neighbors.Remove(node.Id);
            }

            graph.Nodes.Remove(node.Id);
        }
    }

    private static void UpdateDifficultyAndRewards(ProcRoomGraph graph)
    {
        foreach (var node in graph.Nodes.Values)
        {
            var difficulty = node.Depth;
            if (node.Type == ProcRoomType.Elite) difficulty += 2;
            if (node.Type == ProcRoomType.Boss) difficulty += 4;
            node.Difficulty = difficulty;
            node.RewardTier = node.Type is ProcRoomType.Reward or ProcRoomType.Shop ? Math.Max(1, difficulty / 2) : 0;
        }
    }

    private static void Shuffle<T>(Random rng, IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
