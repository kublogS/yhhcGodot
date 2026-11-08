using Godot;

public partial class GameSession
{
    public void ApplyRunContract(
        RunStateContract contract,
        DungeonData? dungeon,
        List<OverworldEnemyModel>? enemies,
        Vector3 playerPosition,
        float playerYaw)
    {
        RunLifecycleService.ApplyLoadedRun(this, contract, dungeon, enemies, playerPosition, playerYaw);
    }

    public void BeginOverworldRun()
    {
        RunLifecycleService.BeginOverworldRun(this);
    }

    public void GenerateNewDungeon()
    {
        RunLifecycleService.GenerateNewDungeon(this);
    }

    public bool TryAdvanceOverworldFloor()
    {
        return RunLifecycleService.TryAdvanceFloor(this);
    }

    public void ReturnToSpawnHub()
    {
        RunLifecycleService.ReturnToSpawnHub(this);
    }
}
