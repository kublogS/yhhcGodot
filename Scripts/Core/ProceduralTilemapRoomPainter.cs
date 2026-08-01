using Godot;

public static class ProceduralTilemapRoomPainter
{
    public static void PaintRooms(ProceduralTilemapBuildContext ctx)
    {
        foreach (var (roomId, embedCell) in ctx.Embed.Positions)
        {
            var node = ctx.Graph.Nodes[roomId];
            var roomSize = ctx.ResolveRoomSize(node.Type);
            var origin = ctx.ResolveRoomOrigin(embedCell, roomSize);
            var bounds = new Rect2I(origin, roomSize);
            ctx.RoomBounds[roomId] = bounds;
            ctx.RoomLevels[roomId] = node.Type is ProcRoomType.Boss or ProcRoomType.Elite ? 1 : 0;
            ctx.RoomBoundaryDescriptors[roomId] = new RoomBoundaryDescriptor { RoomId = roomId };
            PaintSingleRoom(ctx, bounds, roomId);
        }
    }

    private static void PaintSingleRoom(ProceduralTilemapBuildContext ctx, Rect2I bounds, int roomId)
    {
        for (var y = bounds.Position.Y; y < bounds.End.Y; y++)
        {
            for (var x = bounds.Position.X; x < bounds.End.X; x++)
            {
                var isBorder = x == bounds.Position.X
                               || x == bounds.End.X - 1
                               || y == bounds.Position.Y
                               || y == bounds.End.Y - 1;
                ctx.SetTile(x, y, isBorder ? TileType.Wall : TileType.Floor);
                ctx.RoomIdGrid[y, x] = roomId;
            }
        }
    }
}
