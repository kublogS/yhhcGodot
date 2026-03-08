using Godot;

public readonly record struct ResolutionOption(int Width, int Height, string Label)
{
    public Vector2I Size => new(Width, Height);

    public float AspectRatio => Width / (float)Mathf.Max(1, Height);

    public int PixelCount => Width * Height;

    public static ResolutionOption FromSize(Vector2I size)
    {
        return new ResolutionOption(size.X, size.Y, $"{size.X}x{size.Y}");
    }
}
