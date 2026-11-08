using System.Collections.Generic;
using System.Linq;
using Godot;

public static class ProceduralTilemapFeaturePass
{
    public static void Apply(ProceduralTilemapBuildContext ctx)
    {
        AddExit(ctx);
        AddSaveSanctuaries(ctx);
        AddSecretBreakables(ctx);
    }

    private static void AddExit(ProceduralTilemapBuildContext ctx)
    {
        if (!ctx.RoomBounds.TryGetValue(ctx.Graph.BossId, out var bossRoom))
        {
            return;
        }

        var center = ctx.RoomCenter(bossRoom);
        ctx.SetTile(center.X, center.Y, TileType.Exit);
        ctx.ExitTiles.Add(center);
    }

    private static void AddSaveSanctuaries(ProceduralTilemapBuildContext ctx)
    {
        foreach (var roomId in SelectSanctuaryRooms(ctx))
        {
            if (!ctx.RoomBounds.TryGetValue(roomId, out var room))
            {
                continue;
            }

            var tile = FindSaveTile(ctx, room);
            ctx.SetTile(tile.X, tile.Y, TileType.Save);
            ctx.SaveTiles.Add(tile);
            ctx.SaveRoomIds.Add(roomId);
        }
    }

    private static List<int> SelectSanctuaryRooms(ProceduralTilemapBuildContext ctx)
    {
        var ordered = ctx.Graph.Nodes.Keys
            .Where(id => id != ctx.Graph.StartId)
            .OrderBy(id => id)
            .ToList();
        var picks = new List<int>();
        for (var i = 0; i < ordered.Count; i++)
        {
            if ((i + 1) % 5 == 0)
            {
                picks.Add(ordered[i]);
            }
        }

        if (!picks.Contains(ctx.Graph.BossId))
        {
            picks.Add(ctx.Graph.BossId);
        }

        return picks;
    }

    private static Vector2I FindSaveTile(ProceduralTilemapBuildContext ctx, Rect2I room)
    {
        var center = ctx.RoomCenter(room);
        var offsets = new[] { Vector2I.Zero, new Vector2I(1, 0), new Vector2I(-1, 0), new Vector2I(0, 1), new Vector2I(0, -1) };
        foreach (var offset in offsets)
        {
            var tile = center + offset;
            if (!ctx.InBounds(tile.X, tile.Y, 1))
            {
                continue;
            }

            var current = ctx.GetTile(tile.X, tile.Y);
            if (current is TileType.Floor or TileType.Threshold or TileType.Doorway)
            {
                return tile;
            }
        }

        return center;
    }

    private static void AddSecretBreakables(ProceduralTilemapBuildContext ctx)
    {
        foreach (var (roomId, room) in ctx.RoomBounds)
        {
            if (roomId == ctx.Graph.StartId || roomId == ctx.Graph.BossId || ctx.Rng.NextDouble() > 0.22)
            {
                continue;
            }

            var wall = new Vector2I(room.Position.X + (room.Size.X / 2), room.Position.Y);
            var pocket = wall + new Vector2I(0, -1);
            if (!ctx.InBounds(wall.X, wall.Y, 2) || !ctx.InBounds(pocket.X, pocket.Y, 2))
            {
                continue;
            }

            if (ctx.GetTile(wall.X, wall.Y) != TileType.Wall || ctx.GetTile(pocket.X, pocket.Y) != TileType.Void)
            {
                continue;
            }

            ctx.SetTile(wall.X, wall.Y, TileType.Breakable);
            ctx.BreakableTiles.Add(wall);
            CarvePocket(ctx, pocket);
        }
    }

    private static void CarvePocket(ProceduralTilemapBuildContext ctx, Vector2I origin)
    {
        for (var y = origin.Y - 1; y <= origin.Y + 1; y++)
        {
            for (var x = origin.X - 1; x <= origin.X + 1; x++)
            {
                if (!ctx.InBounds(x, y, 1))
                {
                    continue;
                }

                var border = x == origin.X - 1 || x == origin.X + 1 || y == origin.Y - 1 || y == origin.Y + 1;
                ctx.SetTile(x, y, border ? TileType.Wall : TileType.Floor);
            }
        }
    }
}
