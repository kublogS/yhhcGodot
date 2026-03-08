using System.Collections.Generic;

public static class ProceduralGenerationValidator
{
    public static List<string> ValidateGraph(ProcRoomGraph graph)
    {
        var errors = new List<string>();
        if (!graph.Nodes.ContainsKey(graph.StartId)) errors.Add("missing start");
        if (!graph.Nodes.ContainsKey(graph.BossId)) errors.Add("missing boss");
        if (graph.Nodes.TryGetValue(graph.BossId, out var boss) && boss.Neighbors.Count == 0)
        {
            errors.Add("boss has no neighbors");
        }

        return errors;
    }

    public static List<string> ValidateEmbedding(ProcRoomGraph graph, ProcEmbedResult embed)
    {
        var errors = new List<string>();
        if (embed.Positions.Count != graph.Nodes.Count)
        {
            errors.Add("not all rooms placed");
        }

        return errors;
    }
}
