using System.Collections.Generic;
using Godot;

public sealed class ExploreRuntimeBuildResult
{
    public SpawnHubLayout? SpawnHubLayout { get; set; }
    public Node3D? ManualPanel { get; set; }
    public List<EnemyAgent> EnemyAgents { get; } = new();
    public OverworldEnemyRuntimeCoordinator? EnemyRuntime { get; set; }
}

public sealed class ExploreRuntimeSceneBuilder
{
    private readonly GameSession _session;
    private readonly ExploreSceneAssembler _sceneAssembler;
    private readonly PlayerController _player;
    private readonly Node3D _dungeonRoot;
    private readonly Node3D _enemiesRoot;
    private readonly PauseOverlayController _pause;
    private readonly ManualOverlayController _manual;
    private readonly MapOverlayController _map;
    private readonly Node3D _owner;

    public ExploreRuntimeSceneBuilder(
        GameSession session,
        ExploreSceneAssembler sceneAssembler,
        PlayerController player,
        Node3D dungeonRoot,
        Node3D enemiesRoot,
        PauseOverlayController pause,
        ManualOverlayController manual,
        MapOverlayController map,
        Node3D owner)
    {
        _session = session;
        _sceneAssembler = sceneAssembler;
        _player = player;
        _dungeonRoot = dungeonRoot;
        _enemiesRoot = enemiesRoot;
        _pause = pause;
        _manual = manual;
        _map = map;
        _owner = owner;
    }

    public ExploreRuntimeBuildResult BuildSpawnHub(Node3D? manualPanel)
    {
        var layout = _sceneAssembler.BuildSpawnHub(_session, _dungeonRoot, _enemiesRoot, _player, ref manualPanel);
        _player.SetTorchEnabled(false);
        ExploreLightingRig.SetDungeonMood(_owner, false);
        ResetOverlayState();
        return new ExploreRuntimeBuildResult
        {
            SpawnHubLayout = layout,
            ManualPanel = manualPanel,
        };
    }

    public ExploreRuntimeBuildResult BuildDungeon(Node3D? manualPanel)
    {
        var result = new ExploreRuntimeBuildResult();
        if (_session.CurrentDungeon is null)
        {
            return result;
        }

        if (_session.State is not null)
        {
            result.EnemyRuntime = new OverworldEnemyRuntimeCoordinator(_session.State.Rng);
        }

        result.EnemyAgents.AddRange(_sceneAssembler.BuildDungeon(
            _session,
            _dungeonRoot,
            _enemiesRoot,
            _player,
            ref manualPanel,
            result.EnemyRuntime));
        result.ManualPanel = manualPanel;
        ExploreEncounterWorldSync.RefreshEnemyRuntime(result.EnemyRuntime, _player, _session.CurrentDungeon, _session.OverworldEnemies);
        _player.SetTorchEnabled(true);
        ExploreLightingRig.SetDungeonMood(_owner, true);
        ResetOverlayState();
        return result;
    }

    private void ResetOverlayState()
    {
        _manual.LoadZone("INDICE");
        _manual.SetOpen(false);
        _map.Visible = false;
        _pause.SetOverlayVisible(false);
        ExploreModalFlow.ApplyInputLock(_player, _pause, _manual, _map);
    }
}
