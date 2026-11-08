using System;
using Godot;

// Builds a mono 16-bit PCM AudioStreamWav from a plain amplitude-at-time
// function - used to synthesize short SFX (grunts, hits, chimes) in code
// instead of shipping/downloading recorded audio, matching the project's
// procedural-first approach used for the torch model and enemy meshes.
public static class ProceduralAudioSynth
{
    public static AudioStreamWav Build(float durationSec, int sampleRate, Func<float, float> amplitudeAt)
    {
        var sampleCount = Math.Max(1, (int)(durationSec * sampleRate));
        var data = new byte[sampleCount * 2];
        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleRate;
            var sample = Mathf.Clamp(amplitudeAt(t), -1f, 1f);
            var pcm = (short)(sample * short.MaxValue);
            data[i * 2] = (byte)(pcm & 0xFF);
            data[(i * 2) + 1] = (byte)((pcm >> 8) & 0xFF);
        }

        return new AudioStreamWav
        {
            Data = data,
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = sampleRate,
            Stereo = false,
        };
    }
}
