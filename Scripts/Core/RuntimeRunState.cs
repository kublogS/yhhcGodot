using System;

public sealed class RuntimeRunState
{
    private int _maxFloors = 20;

    public bool IsActive { get; set; }
    public RunLocation Location { get; set; } = RunLocation.Indice;
    public int Seed { get; set; }
    public int Floor { get; set; }
    public int MaxFloors
    {
        get => _maxFloors;
        set => _maxFloors = Math.Max(1, value <= 0 ? 20 : value);
    }

    public bool HasOverworldRuntime => IsActive && Location == RunLocation.Overworld && Seed > 0;

    public void Apply(RunStateContract contract)
    {
        IsActive = contract.IsRunActive;
        Location = contract.Location;
        Seed = Math.Max(0, contract.ProceduralSeed);
        Floor = Math.Max(0, contract.ProceduralFloor);
        MaxFloors = contract.ProceduralMaxFloors;
    }

    public void Reset(int maxFloors = 20)
    {
        Apply(RunStatePolicy.ReturnToIndice(maxFloors));
    }

    public RunStateContract ToContract()
    {
        return new RunStateContract
        {
            IsRunActive = IsActive,
            Location = Location,
            ProceduralSeed = Seed,
            ProceduralFloor = Floor,
            ProceduralMaxFloors = MaxFloors,
        };
    }

    public static RuntimeRunState From(RunStateContract contract)
    {
        var state = new RuntimeRunState();
        state.Apply(contract);
        return state;
    }
}
