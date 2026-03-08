public sealed class AsciiConversionSettings
{
    public int Columns { get; init; } = 72;
    public float CharacterAspect { get; init; } = 0.5f;
    public float Contrast { get; init; } = 1.0f;
    public float BrightnessOffset { get; init; }
    public bool Invert { get; init; }
    public bool RespectAlpha { get; init; } = true;
}
