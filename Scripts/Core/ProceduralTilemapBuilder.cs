public sealed class ProceduralTilemapBuilder
{
    private readonly ProceduralTilemapBuildContext _context;

    public ProceduralTilemapBuilder(ProcRoomGraph graph, ProcEmbedResult embed, int seed, int gap = 1)
    {
        _context = new ProceduralTilemapBuildContext(graph, embed, seed, gap);
    }

    public ProcTilemapResult Build()
    {
        ProceduralTilemapRoomPainter.PaintRooms(_context);
        ProceduralTilemapCorridorCarver.ConnectGraph(_context);
        ProceduralTilemapFeaturePass.Apply(_context);
        ProceduralTilemapTopologyPass.Finalize(_context);
        return _context.ToResult();
    }
}
