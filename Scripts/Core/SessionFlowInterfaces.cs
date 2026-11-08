using System.Collections.Generic;
using Godot;

public interface ISessionSnapshotHost
{
    GameState? State { get; set; }
    int CurrentSlot { get; set; }
    DungeonData? CurrentDungeon { get; set; }
    List<OverworldEnemyModel> OverworldEnemies { get; set; }
    Vector3 PlayerWorldPosition { get; set; }
    float PlayerYawRadians { get; set; }
    string LastEncounterContext { get; set; }
    List<string> LastBattleLog { get; }
    bool HasPendingMoveReplace { get; set; }
    MoveModel? PendingMove { get; set; }
    int PendingReplaceIndex { get; set; }
    int DeepestFloor { get; set; }
    int LabyrinthCompletions { get; set; }
    RuntimeRunState CurrentRun { get; }
}

public interface INewGameSessionContext : ISessionSnapshotHost
{
    void CreateNewGame(string playerName);
}

public interface IRewardSessionContext : ISessionSnapshotHost
{
    (int Exp, int Soli, int Tokens, int Items) FinalizeBattleRewards();
    void ClearAfterReward();
    MoveModel? PickStealableMove();
    bool TryAddMoveToPlayer(MoveModel move);
    int XpNeededForLevel(int level);
}

public interface IBattleSessionContext
{
    GameState? State { get; set; }
    (int Exp, int Soli, int Tokens, int Items) FinalizeBattleRewards();
}
