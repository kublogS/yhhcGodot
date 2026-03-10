using System.Collections.Generic;
using Godot;

public sealed class SlotMeta
{
    public bool IsUsed { get; set; }
    public int SlotId { get; set; }
    public int SaveVersion { get; set; } = 1;
    public long LastSaveTs { get; set; }
    public double PlaytimeSeconds { get; set; }
}

public sealed class HudPreview
{
    public string CharacterName { get; set; } = "";
    public int DeepestFloor { get; set; }
    public int LabyrinthCompletions { get; set; }
    public double PlaytimeSeconds { get; set; }
}

public sealed class SavedWorld
{
    public int Width { get; set; }
    public int Height { get; set; }
    public List<int> GridFlat { get; set; } = new();
    public int Seed { get; set; }
    public int FloorIndex { get; set; }
    public float PlayerX { get; set; }
    public float PlayerY { get; set; }
    public float PlayerZ { get; set; }
    public float PlayerYaw { get; set; }
}

public sealed class SavedGameRng
{
    public uint RngState { get; set; }
}

public sealed class SavedRuntime
{
    public SavedWorld World { get; set; } = new();
    public List<OverworldEnemyModel> OverworldEnemies { get; set; } = new();
    public string LastEncounterContext { get; set; } = "";
    public int DeepestFloor { get; set; }
    public int LabyrinthCompletions { get; set; }
    public bool HasEnteredOverworld { get; set; }
    public bool RunActive { get; set; }
    public int ProceduralSeed { get; set; }
    public int ProceduralFloor { get; set; }
    public int ProceduralMaxFloors { get; set; } = 20;
}

public sealed class SaveFileData
{
    public SlotMeta Meta { get; set; } = new();
    public HudPreview HudPreview { get; set; } = new();
    public GameState? GameState { get; set; }
    public SavedRuntime Runtime { get; set; } = new();
    public SavedGameRng Rng { get; set; } = new();
}

public sealed class SaveSlotView
{
    public int SlotId { get; init; }
    public bool IsUsed { get; init; }
    public string CharacterName { get; init; } = "";
    public int DeepestFloor { get; init; }
    public int LabyrinthCompletions { get; init; }
    public string LastSaveText { get; init; } = "-";
}

public enum SaveCopyResult
{
    Success = 0,
    InvalidSlot,
    SameSlot,
    SourceEmpty,
    TargetOccupied,
}
