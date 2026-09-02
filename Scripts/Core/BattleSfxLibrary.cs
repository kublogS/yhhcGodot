using System;
using System.Collections.Generic;
using Godot;

// Most battle SFX are synthesized procedurally on first use and cached (no
// downloaded/recorded audio assets) - simple oscillator/noise DSP in the
// spirit of classic chiptune sound design, matching the project's other
// procedural-first assets (torch model, enemy meshes). The synthesis is
// deterministic and only ever runs once per clip, so it never touches
// gameplay RNG (state.Rng) and has no effect on the seeded-determinism
// regression tests. The victory jingle is the one exception: a recorded
// bell sample (res://Assets/audio/victory_bell.wav), loaded once and cached.
public static class BattleSfxLibrary
{
    private const int SampleRate = 22050;
    private const string VictoryJinglePath = "res://Assets/audio/victory_bell.wav";
    private static readonly Random NoiseRng = new(1);
    private static readonly Dictionary<string, AudioStreamWav> GruntCache = new();
    private static AudioStreamWav? _hitImpact;
    private static AudioStreamWav? _criticalHit;
    private static AudioStream? _victoryJingle;

    private readonly record struct GruntProfile(float FreqStart, float FreqEnd, float Duration, float NoiseMix, float ClickRateHz);

    // Fundamental pitch/noise mix per species gives each monster a distinct
    // "voice" without needing real vocal recordings: goblin is a sharp high
    // yip, orco a low guttural growl, scheletro almost pure clicking noise
    // (bone rattle, not a vocal grunt at all), slime a wet tonal glissando,
    // bandito a punchy human exhale, stregone an airier vibrato "hum".
    private static readonly Dictionary<string, GruntProfile> GruntProfiles = new()
    {
        ["goblin"] = new GruntProfile(190f, 130f, 0.16f, 0.55f, 0f),
        ["orco"] = new GruntProfile(80f, 60f, 0.36f, 0.6f, 0f),
        ["scheletro"] = new GruntProfile(260f, 220f, 0.14f, 0.9f, 26f),
        ["slime"] = new GruntProfile(300f, 120f, 0.24f, 0.15f, 0f),
        ["bandito"] = new GruntProfile(130f, 100f, 0.18f, 0.45f, 0f),
        ["stregone"] = new GruntProfile(170f, 190f, 0.3f, 0.3f, 0f),
    };

    public static AudioStream GetMonsterGrunt(string? spriteKey)
    {
        var key = spriteKey is not null && GruntProfiles.ContainsKey(spriteKey) ? spriteKey : "goblin";
        if (GruntCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var stream = SynthesizeGrunt(GruntProfiles[key]);
        GruntCache[key] = stream;
        return stream;
    }

    public static AudioStream GetHitImpact()
    {
        return _hitImpact ??= ProceduralAudioSynth.Build(0.18f, SampleRate, t =>
        {
            var toneEnv = MathF.Exp(-t * 26f);
            var tone = MathF.Sin(2f * MathF.PI * 120f * t) * toneEnv;
            var noiseEnv = MathF.Exp(-t * 55f);
            var noise = (((float)NoiseRng.NextDouble() * 2f) - 1f) * noiseEnv;
            return (tone * 0.7f) + (noise * 0.5f);
        });
    }

    public static AudioStream GetCriticalHit()
    {
        var notes = new[] { 659.25f, 830.61f, 1046.5f };
        return _criticalHit ??= ProceduralAudioSynth.Build(0.32f, SampleRate, t => Arpeggio(t, notes, 0.1f));
    }

    public static AudioStream GetVictoryJingle()
    {
        return _victoryJingle ??= GD.Load<AudioStream>(VictoryJinglePath);
    }

    private static AudioStreamWav SynthesizeGrunt(GruntProfile profile)
    {
        return ProceduralAudioSynth.Build(profile.Duration, SampleRate, t =>
        {
            var progress = Mathf.Clamp(t / profile.Duration, 0f, 1f);
            var freq = Mathf.Lerp(profile.FreqStart, profile.FreqEnd, progress);
            var attack = Mathf.Clamp(t / 0.02f, 0f, 1f);
            var decay = MathF.Exp(-t * (3.2f / profile.Duration));
            var env = attack * decay;

            var tone = MathF.Sin(2f * MathF.PI * freq * t);
            var noise = ((float)NoiseRng.NextDouble() * 2f) - 1f;
            if (profile.ClickRateHz > 0f)
            {
                var clickPhase = (t * profile.ClickRateHz) % 1f;
                noise *= clickPhase < 0.35f ? 1f : 0.15f;
            }

            var voiced = (tone * (1f - profile.NoiseMix)) + (noise * profile.NoiseMix);
            return voiced * env;
        });
    }

    private static float Arpeggio(float t, float[] notes, float noteDuration)
    {
        var index = Math.Min(notes.Length - 1, (int)(t / noteDuration));
        var localT = t - (index * noteDuration);
        var env = MathF.Exp(-localT * 16f);
        var f = notes[index];
        return ((MathF.Sin(2f * MathF.PI * f * localT) * 0.6f) + (MathF.Sin(2f * MathF.PI * f * 2f * localT) * 0.3f)) * env;
    }
}
