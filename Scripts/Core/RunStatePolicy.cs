using System;

public static class RunStatePolicy
{
    public static RunStateContract EnterOverworld(int proceduralSeed, int proceduralFloor, int maxFloors)
    {
        return new RunStateContract
        {
            IsRunActive = true,
            Location = RunLocation.Overworld,
            ProceduralSeed = Math.Max(0, proceduralSeed),
            ProceduralFloor = Math.Max(0, proceduralFloor),
            ProceduralMaxFloors = Math.Max(1, maxFloors),
        };
    }

    public static RunStateContract ReturnToIndice(int maxFloors)
    {
        return new RunStateContract
        {
            IsRunActive = false,
            Location = RunLocation.Indice,
            ProceduralSeed = 0,
            ProceduralFloor = 0,
            ProceduralMaxFloors = Math.Max(1, maxFloors),
        };
    }

    public static RunStateContract AdvanceFloor(RunStateContract current, out bool runCompleted)
    {
        runCompleted = false;
        if (!current.IsRunActive || current.Location != RunLocation.Overworld)
        {
            return current;
        }

        var maxFloors = Math.Max(1, current.ProceduralMaxFloors);
        var nextFloor = current.ProceduralFloor + 1;
        if (nextFloor >= maxFloors)
        {
            runCompleted = true;
            return ReturnToIndice(maxFloors);
        }

        return EnterOverworld(current.ProceduralSeed, nextFloor, maxFloors);
    }
}
