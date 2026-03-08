using Godot;

public static class CombatDebugTest
{
    public static string Run()
    {
        var state = GameState.New("Debug", 12345);
        state.Enemies = new System.Collections.Generic.List<CharacterModel>
        {
            EnemyCatalog.EnemyFromSpec(EnemyCatalog.Mobs[0], state.Rng, 1f, string.Empty, "Mob"),
            EnemyCatalog.EnemyFromSpec(EnemyCatalog.Mobs[1], state.Rng, 1f, string.Empty, "Mob"),
        };
        state.SyncEnemyLegacy();

        var turn = CombatService.CombatTurn(state, new CombatTurnRequest
        {
            ActionType = CombatActionType.Attack,
            SelectedMoveIndex = 0,
            SelectedTargetIndex = 0,
        });

        var text = string.Join("\n", turn.LogLines);
        GD.Print($"[CombatDebugTest]\n{text}");
        return text;
    }
}
