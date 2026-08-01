using Godot;

public static class ProceduralTilemapCorridorCarver
{
    public static void ConnectGraph(ProceduralTilemapBuildContext ctx)
    {
        foreach (var node in ctx.Graph.Nodes.Values)
        {
            foreach (var neighbor in node.Neighbors)
            {
                if (node.Id >= neighbor || !ctx.RoomBounds.ContainsKey(neighbor))
                {
                    continue;
                }

                ctx.RegisterNeighbor(node.Id, neighbor);
                var a = ctx.RoomCenter(ctx.RoomBounds[node.Id]);
                var b = ctx.RoomCenter(ctx.RoomBounds[neighbor]);
                CarvePair(ctx, a, b);
            }
        }
    }

    private static void CarvePair(ProceduralTilemapBuildContext ctx, Vector2I a, Vector2I b)
    {
        var horizontalFirst = ctx.Rng.NextDouble() < 0.5;
        var pivot = horizontalFirst ? new Vector2I(b.X, a.Y) : new Vector2I(a.X, b.Y);
        CarveLine(ctx, a, pivot);
        CarveLine(ctx, pivot, b);
        PlaceOccasionalBreakableGate(ctx, a, b);
    }

    private static void CarveLine(ProceduralTilemapBuildContext ctx, Vector2I from, Vector2I to)
    {
        if (from.X == to.X)
        {
            var step = to.Y >= from.Y ? 1 : -1;
            for (var y = from.Y; y != to.Y + step; y += step)
            {
                CarveCell(ctx, from.X, y);
            }
            return;
        }

        var xStep = to.X >= from.X ? 1 : -1;
        for (var x = from.X; x != to.X + xStep; x += xStep)
        {
            CarveCell(ctx, x, from.Y);
        }
    }

    private static void CarveCell(ProceduralTilemapBuildContext ctx, int x, int y)
    {
        if (!ctx.InBounds(x, y, 1))
        {
            return;
        }

        var current = ctx.GetTile(x, y);
        if (current == TileType.Exit || current == TileType.Save)
        {
            return;
        }

        ctx.SetTile(x, y, TileType.Floor);
        ctx.CorridorTiles.Add(new Vector2I(x, y));
    }

    private static void PlaceOccasionalBreakableGate(ProceduralTilemapBuildContext ctx, Vector2I a, Vector2I b)
    {
        if (ctx.Rng.NextDouble() > 0.16)
        {
            return;
        }

        var gate = new Vector2I((a.X + b.X) / 2, (a.Y + b.Y) / 2);
        if (!ctx.InBounds(gate.X, gate.Y, 2) || ctx.GetTile(gate.X, gate.Y) != TileType.Floor)
        {
            return;
        }

        var side = ctx.Rng.Next(0, 2) == 0 ? new Vector2I(1, 0) : new Vector2I(0, 1);
        var wall = gate + side;
        var pocket = wall + side;
        if (!ctx.InBounds(wall.X, wall.Y, 2) || !ctx.InBounds(pocket.X, pocket.Y, 2))
        {
            return;
        }

        if (ctx.GetTile(wall.X, wall.Y) != TileType.Wall && ctx.GetTile(wall.X, wall.Y) != TileType.Void)
        {
            return;
        }

        if (ctx.GetTile(pocket.X, pocket.Y) != TileType.Void && ctx.GetTile(pocket.X, pocket.Y) != TileType.Wall)
        {
            return;
        }

        ctx.SetTile(wall.X, wall.Y, TileType.Breakable);
        ctx.BreakableTiles.Add(wall);
        ctx.SetTile(pocket.X, pocket.Y, TileType.Floor);
    }
}
