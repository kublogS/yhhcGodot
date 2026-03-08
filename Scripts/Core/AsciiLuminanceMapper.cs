using Godot;

public sealed class AsciiLuminanceMapper
{
    private readonly string _ramp;

    public AsciiLuminanceMapper(string? ramp = null)
    {
        _ramp = string.IsNullOrWhiteSpace(ramp) ? "@%#*+=-:. " : ramp;
    }

    public char FromLuminance(float luminance, bool invert = false)
    {
        var value = Mathf.Clamp(luminance, 0f, 1f);
        if (invert)
        {
            value = 1f - value;
        }

        var index = (int)Mathf.Round(value * (_ramp.Length - 1));
        index = Mathf.Clamp(index, 0, _ramp.Length - 1);
        return _ramp[index];
    }
}
