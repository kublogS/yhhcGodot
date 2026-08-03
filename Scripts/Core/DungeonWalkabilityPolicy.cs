public static class DungeonWalkabilityPolicy
{
    public static TileTraversalState Resolve(TileType tile, bool isBreakableBroken)
    {
        if (tile == TileType.Breakable)
        {
            return isBreakableBroken ? TileTraversalState.BreakableBroken : TileTraversalState.BreakableIntact;
        }

        return tile switch
        {
            TileType.Floor => TileTraversalState.Walkable,
            TileType.Doorway => TileTraversalState.Walkable,
            TileType.Threshold => TileTraversalState.Walkable,
            TileType.Exit => TileTraversalState.Walkable,
            TileType.Save => TileTraversalState.Walkable,
            _ => TileTraversalState.Solid,
        };
    }

    public static bool IsWalkable(TileType tile, bool isBreakableBroken = false)
    {
        var state = Resolve(tile, isBreakableBroken);
        return state is TileTraversalState.Walkable or TileTraversalState.BreakableBroken;
    }
}
