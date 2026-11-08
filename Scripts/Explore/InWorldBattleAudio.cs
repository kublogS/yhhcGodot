using Godot;

// Plays battle SFX: monster grunts and hit impacts are positional
// (AudioStreamPlayer3D at the acting/hit participant), the player's
// critical-hit sting is a flat UI-level cue instead - it is feedback about
// the player's own action, not something happening at a place in the world.
public sealed class InWorldBattleAudio
{
    private readonly Node3D _root;
    private readonly AudioStreamPlayer _uiPlayer;

    public InWorldBattleAudio(Node3D root)
    {
        _root = root;
        _uiPlayer = new AudioStreamPlayer { Name = "BattleUiSfx" };
        root.AddChild(_uiPlayer);
    }

    public void PlayGrunt(InWorldBattleParticipant participant, string? spriteKey)
    {
        PlayAt(participant.Root.GlobalPosition, BattleSfxLibrary.GetMonsterGrunt(spriteKey));
    }

    public void PlayHitImpact(InWorldBattleParticipant participant)
    {
        PlayAt(participant.Root.GlobalPosition, BattleSfxLibrary.GetHitImpact());
    }

    public void PlayCriticalHit()
    {
        _uiPlayer.Stream = BattleSfxLibrary.GetCriticalHit();
        _uiPlayer.Play();
    }

    private void PlayAt(Vector3 position, AudioStream stream)
    {
        var player = new AudioStreamPlayer3D
        {
            Stream = stream,
            MaxDistance = 20f,
            UnitSize = 4f,
        };
        _root.AddChild(player);
        player.GlobalPosition = position;
        player.Play();
        player.Finished += player.QueueFree;
    }
}
