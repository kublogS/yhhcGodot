using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public sealed class InWorldBattlePresentationController
{
    private readonly Node3D _root;
    private readonly PlayerController _player;
    private readonly InWorldBattleCameraRig _cameraRig;
    private readonly Dictionary<string, InWorldBattleParticipant> _participants = new();
    private readonly Dictionary<EnemyAgent, bool> _enemyVisibility = new();
    private readonly Dictionary<EnemyAgent, Node.ProcessModeEnum> _enemyProcessModes = new();
    private Vector3 _battleCenter;
    private Vector3 _battleForward = Vector3.Forward;
    private Vector3 _battleRight = Vector3.Right;

    public InWorldBattlePresentationController(Node3D root, Camera3D battleCamera, PlayerController player)
    {
        _root = root;
        _player = player;
        _cameraRig = new InWorldBattleCameraRig(battleCamera, player.GetViewCamera());
    }

    public bool IsActive => _root.Visible;

    public bool BeginEncounter(GameSession session, IReadOnlyList<EnemyAgent> enemyAgents)
    {
        if (session.State is null)
        {
            return false;
        }

        ClearParticipants();
        var involvedAgents = InWorldBattlePresentationLayout.ResolveInvolvedAgents(session, enemyAgents);
        if (involvedAgents.Count == 0)
        {
            return false;
        }

        HideWorldEnemies(enemyAgents);
        _battleCenter = InWorldBattlePresentationLayout.ComputeBattleCenter(_player.GlobalPosition, involvedAgents);
        _battleForward = InWorldBattlePresentationLayout.ComputeBattleForward(_player.GlobalPosition, involvedAgents);
        _battleRight = Vector3.Up.Cross(_battleForward).Normalized();
        if (_battleRight.LengthSquared() < 0.001f)
        {
            _battleRight = Vector3.Right;
        }

        _root.Visible = true;
        _participants[CombatService.PlayerActorId] = InWorldBattleParticipantFactory.CreatePlayer(_root);
        foreach (var enemy in session.EncounterWorldEnemies)
        {
            if (!string.IsNullOrWhiteSpace(enemy.EnemyId))
            {
                _participants[enemy.EnemyId] = InWorldBattleParticipantFactory.CreateEnemy(_root, enemy);
            }
        }

        LayoutBattleState(session.State, instant: true);
        UpdateTargetSelection(session.State, 0);
        var cameraPosition = _battleCenter - (_battleForward * 6.2f) + (_battleRight * 2.1f) + new Vector3(0f, 3.8f, 0f);
        _cameraRig.Activate(cameraPosition, _battleCenter + new Vector3(0f, 1.25f, 0f));
        return true;
    }

    public void Tick(float dt)
    {
        if (!_root.Visible)
        {
            return;
        }

        _cameraRig.Tick(dt);
        foreach (var participant in _participants.Values)
        {
            participant.Tick(dt);
        }
    }

    public void UpdateTargetSelection(GameState? state, int selectedTarget)
    {
        if (!_root.Visible || state is null || state.Enemies.Count == 0)
        {
            return;
        }

        var clamped = Mathf.Clamp(selectedTarget, 0, state.Enemies.Count - 1);
        var selectedId = InWorldBattlePresentationLayout.ResolveEnemyActorId(state.Enemies[clamped], clamped);
        foreach (var pair in _participants)
        {
            pair.Value.SetSelected(pair.Key == selectedId);
        }

        if (_participants.TryGetValue(selectedId, out var target))
        {
            _cameraRig.Focus(target.Root.GlobalPosition + new Vector3(0f, 1.1f, 0f));
        }
    }

    public async Task PresentEventAsync(CombatEventEntry battleEvent, GameState? state)
    {
        if (!_root.Visible)
        {
            return;
        }

        _participants.TryGetValue(battleEvent.SourceId ?? string.Empty, out var source);
        _participants.TryGetValue(battleEvent.TargetId ?? string.Empty, out var target);
        switch (battleEvent.EventType)
        {
            case CombatEventType.ActionUsed when source is not null:
                await InWorldBattlePresentationPlayback.DashAsync(_root, source, target, _battleCenter);
                break;
            case CombatEventType.DamageDealt when target is not null:
                await InWorldBattlePresentationPlayback.ImpactAsync(_root, target, battleEvent.Amount);
                break;
            case CombatEventType.DefendActivated when source is not null:
                await InWorldBattlePresentationPlayback.DefendPulseAsync(_root, source);
                break;
            case CombatEventType.FleeSucceeded when source is not null:
                await InWorldBattlePresentationPlayback.FleeAsync(_root, source);
                break;
            case CombatEventType.EnemyKnockedOut when target is not null:
                await InWorldBattlePresentationPlayback.DefeatAsync(_root, target);
                break;
            case CombatEventType.EnemyPromotedFromQueue:
                if (state is not null)
                {
                    LayoutBattleState(state, instant: false);
                }
                break;
        }
    }

    public void EndEncounter()
    {
        RestoreWorldEnemies();
        ClearParticipants();
        _root.Visible = false;
        _cameraRig.Deactivate();
    }

    private void LayoutBattleState(GameState state, bool instant)
    {
        InWorldBattlePresentationLayout.Apply(state, _participants, _battleCenter, _battleForward, _battleRight);
        if (!instant)
        {
            UpdateTargetSelection(state, 0);
        }
    }

    private void HideWorldEnemies(IReadOnlyList<EnemyAgent> enemyAgents)
    {
        _enemyVisibility.Clear();
        _enemyProcessModes.Clear();
        foreach (var agent in enemyAgents)
        {
            _enemyVisibility[agent] = agent.Visible;
            _enemyProcessModes[agent] = agent.ProcessMode;
            agent.Visible = false;
            agent.ProcessMode = Node.ProcessModeEnum.Disabled;
        }
    }

    private void RestoreWorldEnemies()
    {
        foreach (var pair in _enemyVisibility)
        {
            if (!GodotObject.IsInstanceValid(pair.Key))
            {
                continue;
            }

            pair.Key.Visible = pair.Value;
            pair.Key.ProcessMode = _enemyProcessModes[pair.Key];
        }

        _enemyVisibility.Clear();
        _enemyProcessModes.Clear();
    }

    private void ClearParticipants()
    {
        foreach (var child in _root.GetChildren())
        {
            if (child is Node node)
            {
                node.QueueFree();
            }
        }

        _participants.Clear();
    }
}
