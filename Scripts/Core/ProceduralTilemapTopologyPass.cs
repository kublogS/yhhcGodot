using Godot;

public static class ProceduralTilemapTopologyPass
{
    private static readonly Vector2I[] Neighbor8 =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
    };

    public static void Finalize(ProceduralTilemapBuildContext ctx)
    {
        EncloseWalkable(ctx);
        ApplyOuterFrame(ctx);
    }

    private static void EncloseWalkable(ProceduralTilemapBuildContext ctx)
    {
        for (var y = 1; y < ctx.Height - 1; y++)
        {
            for (var x = 1; x < ctx.Width - 1; x++)
            {
                if (!DungeonWalkabilityPolicy.IsWalkable(ctx.GetTile(x, y)))
                {
                    continue;
                }

                foreach (var dir in Neighbor8)
                {
                    var nx = x + dir.X;
                    var ny = y + dir.Y;
                    if (ctx.GetTile(nx, ny) == TileType.Void)
                    {
                        ctx.SetTile(nx, ny, TileType.Wall);
                    }
                }
            }
        }
    }

    private static void ApplyOuterFrame(ProceduralTilemapBuildContext ctx)
    {
        for (var x = 0; x < ctx.Width; x++)
        {
            ctx.SetTile(x, 0, TileType.Wall);
            ctx.SetTile(x, ctx.Height - 1, TileType.Wall);
        }

        for (var y = 0; y < ctx.Height; y++)
        {
            ctx.SetTile(0, y, TileType.Wall);
            ctx.SetTile(ctx.Width - 1, y, TileType.Wall);
        }
    }
}
