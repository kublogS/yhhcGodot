using Godot;

// Music for the Indice hub and MainMenu is a plain looping theme, faded in/out
// on scene transitions. The Overworld instead gets a Minecraft-style ambient
// treatment: the same game theme surfaces every so often while exploring,
// fading in, playing through, and fading out in time to land exactly on the
// clip's natural end - never a hard cut - then goes quiet for a random
// stretch before it plays again. Lives on its own audio bus, same pattern as
// PlayerController's Footsteps bus.
public partial class MusicService : Node
{
    public static MusicService Instance { get; private set; } = null!;

    private const string MusicBusName = "Music";
    private const string IndiceThemePath = "res://Assets/audio/music/indice_theme.mp3";
    private const string GameThemePath = "res://Assets/audio/music/game_theme.mp3";

    private const float TargetVolumeDb = -8f;
    private const float SilentVolumeDb = -80f;
    private const float FadeInSeconds = 2.5f;
    private const float FadeOutSeconds = 1.5f;

    private const float AmbientMinSilenceSeconds = 45f;
    private const float AmbientMaxSilenceSeconds = 110f;
    private const float AmbientFadeInSeconds = 3f;
    private const float AmbientFadeOutSeconds = 4f;

    private readonly RandomNumberGenerator _ambientRng = new();

    private AudioStreamPlayer _loopPlayer = null!;
    private AudioStreamPlayer _ambientPlayer = null!;
    private Tween? _loopFadeTween;
    private Tween? _ambientFadeTween;

    private AudioStream? _indiceTheme;
    private AudioStream? _gameTheme;

    private bool _loopActive;
    private bool _ambientActive;
    private bool _ambientClipPlaying;
    private double _ambientSilenceTimer;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        EnsureMusicBus();
        _ambientRng.Randomize();

        _loopPlayer = new AudioStreamPlayer { Name = "LoopTheme", Bus = MusicBusName, VolumeDb = SilentVolumeDb };
        AddChild(_loopPlayer);
        _loopPlayer.Finished += () =>
        {
            if (_loopActive)
            {
                _loopPlayer.Play();
            }
        };

        _ambientPlayer = new AudioStreamPlayer { Name = "OverworldAmbient", Bus = MusicBusName, VolumeDb = SilentVolumeDb };
        AddChild(_ambientPlayer);
        _ambientPlayer.Finished += OnAmbientClipFinished;

        _indiceTheme = LoadStream(IndiceThemePath);
        _gameTheme = LoadStream(GameThemePath);
    }

    public override void _Process(double delta)
    {
        if (!_ambientActive || _ambientClipPlaying)
        {
            return;
        }

        _ambientSilenceTimer -= delta;
        if (_ambientSilenceTimer <= 0)
        {
            PlayAmbientClip();
        }
    }

    public void FadeInIndiceTheme() => FadeInLoop(_indiceTheme);

    public void FadeInMenuTheme() => FadeInLoop(_gameTheme);

    public void FadeOutAndStop() => FadeOutLoop();

    // Definitely kicks in the instant the player crosses from the Indice into
    // the Overworld; idempotent so advancing floors (which re-triggers this)
    // never interrupts a clip already in progress.
    public void StartOverworldAmbient()
    {
        if (_ambientActive)
        {
            return;
        }

        _ambientActive = true;
        PlayAmbientClip();
    }

    public void StopOverworldAmbient()
    {
        if (!_ambientActive)
        {
            return;
        }

        _ambientActive = false;
        if (!_ambientPlayer.Playing)
        {
            return;
        }

        _ambientFadeTween?.Kill();
        _ambientFadeTween = CreateTween();
        _ambientFadeTween.TweenProperty(_ambientPlayer, "volume_db", SilentVolumeDb, AmbientFadeOutSeconds);
        _ambientFadeTween.TweenCallback(Callable.From(() =>
        {
            _ambientPlayer.Stop();
            _ambientClipPlaying = false;
        }));
    }

    private void FadeInLoop(AudioStream? stream)
    {
        if (stream is null)
        {
            return;
        }

        _loopActive = true;
        _loopFadeTween?.Kill();
        if (_loopPlayer.Stream != stream || !_loopPlayer.Playing)
        {
            _loopPlayer.Stream = stream;
            _loopPlayer.VolumeDb = SilentVolumeDb;
            _loopPlayer.Play();
        }

        _loopFadeTween = CreateTween();
        _loopFadeTween.TweenProperty(_loopPlayer, "volume_db", TargetVolumeDb, FadeInSeconds);
    }

    private void FadeOutLoop()
    {
        _loopActive = false;
        if (!_loopPlayer.Playing)
        {
            return;
        }

        _loopFadeTween?.Kill();
        _loopFadeTween = CreateTween();
        _loopFadeTween.TweenProperty(_loopPlayer, "volume_db", SilentVolumeDb, FadeOutSeconds);
        _loopFadeTween.TweenCallback(Callable.From(_loopPlayer.Stop));
    }

    private void PlayAmbientClip()
    {
        if (_gameTheme is null)
        {
            _ambientClipPlaying = false;
            return;
        }

        _ambientClipPlaying = true;
        _ambientFadeTween?.Kill();
        _ambientPlayer.Stream = _gameTheme;
        _ambientPlayer.VolumeDb = SilentVolumeDb;
        _ambientPlayer.Play();

        var length = (float)_gameTheme.GetLength();
        var hold = Mathf.Max(0f, length - AmbientFadeInSeconds - AmbientFadeOutSeconds);

        _ambientFadeTween = CreateTween();
        _ambientFadeTween.TweenProperty(_ambientPlayer, "volume_db", TargetVolumeDb, AmbientFadeInSeconds);
        _ambientFadeTween.TweenInterval(hold);
        _ambientFadeTween.TweenProperty(_ambientPlayer, "volume_db", SilentVolumeDb, AmbientFadeOutSeconds);
    }

    private void OnAmbientClipFinished()
    {
        _ambientClipPlaying = false;
        _ambientSilenceTimer = _ambientRng.RandfRange(AmbientMinSilenceSeconds, AmbientMaxSilenceSeconds);
    }

    private static AudioStream? LoadStream(string path)
    {
        return ResourceLoader.Exists(path) ? GD.Load<AudioStream>(path) : null;
    }

    private static void EnsureMusicBus()
    {
        for (var i = 0; i < AudioServer.BusCount; i++)
        {
            if (AudioServer.GetBusName(i) == MusicBusName)
            {
                return;
            }
        }

        var index = AudioServer.BusCount;
        AudioServer.AddBus(index);
        AudioServer.SetBusName(index, MusicBusName);
        AudioServer.SetBusSend(index, "Master");
    }
}
