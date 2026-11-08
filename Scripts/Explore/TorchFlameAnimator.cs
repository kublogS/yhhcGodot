using Godot;

// Drives the torch's OmniLight3D flicker: three summed sine waves at
// different frequencies instead of one clean oscillation, so intensity
// reads as an irregular flicker rather than a metronome. Kept as-is (per
// explicit request) when the rest of the torch was rebuilt from scratch -
// the flame's effect on the light in the room doesn't depend on what the
// flame geometry looks like.
public sealed class TorchFlameAnimator
{
    private OmniLight3D? _light;
    private float _baseLightEnergy;
    private float _time;

    public void Capture(OmniLight3D? light)
    {
        _light = light;
        _baseLightEnergy = light?.LightEnergy ?? 0f;
        _time = 0f;
    }

    public void Tick(float dt)
    {
        if (_light is null)
        {
            return;
        }

        _time += dt;
        var flickerA = Mathf.Sin(_time * 11.3f);
        var flickerB = Mathf.Sin((_time * 7.1f) + 1.7f);
        var flickerC = Mathf.Sin((_time * 17.9f) + 4.2f);
        var combined = (flickerA * 0.5f) + (flickerB * 0.3f) + (flickerC * 0.2f);

        _light.LightEnergy = _baseLightEnergy * (1f + (combined * 0.22f));
    }
}
