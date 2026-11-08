using System.Collections.Generic;

public partial class GameSession
{
    public List<OverworldEnemyModel> EncounterWorldEnemies { get; } = new();

    public void CaptureEncounterWorldEnemies(IEnumerable<OverworldEnemyModel> enemies)
    {
        EncounterWorldEnemies.Clear();
        foreach (var enemy in enemies)
        {
            EncounterWorldEnemies.Add(enemy);
        }
    }

    public void RestoreEncounterWorldEnemies()
    {
        foreach (var enemy in EncounterWorldEnemies)
        {
            if (!OverworldEnemies.Contains(enemy))
            {
                OverworldEnemies.Add(enemy);
            }
        }
    }

    public void ClearEncounterWorldEnemies()
    {
        EncounterWorldEnemies.Clear();
    }
}
