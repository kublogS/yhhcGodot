using System.Collections.Generic;
using Godot;

public sealed class EnemyAsciiBillboardFactory
{
    private const string DenseRamp = "@$B%8&WM#*oahkbdpqwmZO0QLCJUYXzcvunxrjft/\\|()1{}[]?-_+~<>i!lI;:,\"^`'. ";
    private readonly Dictionary<string, string> _cache = new();
    private readonly PngImageLoader _imageLoader = new();
    private readonly AsciiArtConverter _converter = new(new AsciiLuminanceMapper(DenseRamp));
    private readonly AsciiArtFormatter _formatter = new();

    public Label3D? Create(string spritePath)
    {
        var ascii = BuildAscii(spritePath);
        if (string.IsNullOrWhiteSpace(ascii))
        {
            return null;
        }

        return new Label3D
        {
            Text = ascii,
            PixelSize = 0.0027f,
            Position = new Vector3(0f, 1.05f, 0f),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            Modulate = PythonColorPalette.GrayLight,
            OutlineSize = 8,
            OutlineModulate = PythonColorPalette.WithAlpha(PythonColorPalette.Black, 216),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            DoubleSided = true,
        };
    }

    private string BuildAscii(string spritePath)
    {
        if (_cache.TryGetValue(spritePath, out var cached))
        {
            return cached;
        }

        var image = _imageLoader.Load(spritePath);
        if (image is null)
        {
            return string.Empty;
        }

        var columns = Mathf.Clamp(image.GetWidth() * 2, 42, 108);
        var lines = _converter.Convert(image, new AsciiConversionSettings
        {
            Columns = columns,
            CharacterAspect = 0.5f,
            Contrast = 1.32f,
            BrightnessOffset = -0.05f,
            RespectAlpha = true,
        });
        var ascii = _formatter.ToMultiline(lines, trimRight: false);
        _cache[spritePath] = ascii;
        return ascii;
    }
}
