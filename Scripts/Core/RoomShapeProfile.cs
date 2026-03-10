using System;

public sealed class RoomShapeProfile
{
    public BoundaryEdgeStyle NorthStyle { get; init; } = BoundaryEdgeStyle.Orthogonal;
    public BoundaryEdgeStyle SouthStyle { get; init; } = BoundaryEdgeStyle.Orthogonal;
    public BoundaryEdgeStyle WestStyle { get; init; } = BoundaryEdgeStyle.Orthogonal;
    public BoundaryEdgeStyle EastStyle { get; init; } = BoundaryEdgeStyle.Orthogonal;
    public int NicheCount { get; init; } = 1;
    public int JaggedPasses { get; init; } = 0;
    public bool UseCornerChamfer { get; init; }

    public RoomBoundaryDescriptor ToDescriptor(int roomId)
    {
        return new RoomBoundaryDescriptor
        {
            RoomId = roomId,
            North = NorthStyle,
            South = SouthStyle,
            West = WestStyle,
            East = EastStyle,
        };
    }

    public static RoomShapeProfile Create(Random rng, bool isBossRoom)
    {
        if (isBossRoom)
        {
            return new RoomShapeProfile
            {
                NorthStyle = BoundaryEdgeStyle.Chamfered,
                SouthStyle = BoundaryEdgeStyle.Chamfered,
                WestStyle = BoundaryEdgeStyle.Orthogonal,
                EastStyle = BoundaryEdgeStyle.Orthogonal,
                NicheCount = 1,
                JaggedPasses = 0,
                UseCornerChamfer = true,
            };
        }

        var styles = new[]
        {
            BoundaryEdgeStyle.Orthogonal,
            BoundaryEdgeStyle.Chamfered,
            BoundaryEdgeStyle.Jagged,
            BoundaryEdgeStyle.FauxCurve,
        };

        return new RoomShapeProfile
        {
            NorthStyle = styles[rng.Next(styles.Length)],
            SouthStyle = styles[rng.Next(styles.Length)],
            WestStyle = styles[rng.Next(styles.Length)],
            EastStyle = styles[rng.Next(styles.Length)],
            NicheCount = rng.Next(1, 4),
            JaggedPasses = rng.Next(0, 3),
            UseCornerChamfer = rng.NextDouble() < 0.65,
        };
    }
}
