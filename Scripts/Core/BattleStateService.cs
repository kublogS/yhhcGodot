using System.Collections.Generic;

public static class BattleStateService
{
    public static void BeginEncounter(GameState state, IReadOnlyList<CharacterModel> enemies, IReadOnlyList<CharacterModel>? queued = null)
    {
        state.ResetBattleInstance();
        state.SetBattleGroups(new List<CharacterModel>(enemies), queued is null ? null : new List<CharacterModel>(queued));
    }

    public static string AddReinforcement(GameState state, CharacterModel enemy)
    {
        var before = state.Enemies.Count;
        state.AddEnemyToBattle(enemy);
        return state.Enemies.Count > before ? "active" : "queue";
    }
}
