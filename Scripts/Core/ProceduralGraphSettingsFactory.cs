using System;

public static class ProceduralGraphSettingsFactory
{
    public static ProcGraphParams CreateForFloor(int seed, int floorIndex)
    {
        var rng = new Random(seed + (floorIndex * 7919));
        var roomTarget = rng.Next(10, 16);
        var depth = Math.Clamp(roomTarget - rng.Next(2, 5), 6, 10);
        var branchChance = 0.4f + ((float)rng.NextDouble() * 0.3f);

        return new ProcGraphParams
        {
            Depth = depth,
            BranchChance = branchChance,
            BranchMin = 1,
            BranchMax = floorIndex < 4 ? 2 : 3,
            MaxDegree = 3,
            ShopDepthMin = 3,
            EliteDepthMin = 4,
            RewardCooldown = 2,
            MinRooms = roomTarget,
            MaxRooms = roomTarget,
        };
    }
}
