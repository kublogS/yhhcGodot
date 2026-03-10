public enum BoundaryEdgeStyle
{
    Orthogonal = 0,
    Chamfered = 1,
    Jagged = 2,
    FauxCurve = 3,
}

public sealed class RoomBoundaryDescriptor
{
    public int RoomId { get; init; }
    public BoundaryEdgeStyle North { get; init; } = BoundaryEdgeStyle.Orthogonal;
    public BoundaryEdgeStyle South { get; init; } = BoundaryEdgeStyle.Orthogonal;
    public BoundaryEdgeStyle West { get; init; } = BoundaryEdgeStyle.Orthogonal;
    public BoundaryEdgeStyle East { get; init; } = BoundaryEdgeStyle.Orthogonal;
}
