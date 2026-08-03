using System.Collections.Generic;
using Godot;

public sealed class ExploreEncounterTrigger
{
    private const float ContactDistance = 1.2f;

    private readonly Node3D _owner;
    private readonly GameSession _session;

    public ExploreEncounterTrigger(Node3D owner, GameSession session)
    {
        _owner = owner;
        _session = session;
    }

    public bool TryStart(PlayerController player, IReadOnlyList<EnemyAgent> enemyAgents, OverworldEnemyRuntimeCoordinator? enemyRuntime)
    {
        foreach (var agent in enemyAgents)
        {
            if (!agent.Model.Active || agent.GlobalPosition.DistanceTo(player.GlobalPosition) >= ContactDistance)
            {
                continue;
            }

            var canTrigger = enemyRuntime is not null
                ? enemyRuntime.ShouldStartEncounterOnContact(agent.Model)
                : OverworldAI.ShouldStartEncounterOnContact(agent.Model, chasing: false, alertTimer: 0f);
            if (!canTrigger)
            {
                continue;
            }

            _session.TryCaptureBattleBackdrop(_owner.GetViewport(), "explore_runtime_snapshot");
            enemyRuntime?.RaiseEncounterAlert();
            _session.StartEncounterWithEnemy(agent.Model);
            return true;
        }

        return false;
    }
}
