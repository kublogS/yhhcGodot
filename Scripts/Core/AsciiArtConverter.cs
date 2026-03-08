using System.Collections.Generic;
using Godot;

public sealed class AsciiArtConverter
{
    private readonly AsciiLuminanceMapper _mapper;

    public AsciiArtConverter(AsciiLuminanceMapper mapper)
    {
        _mapper = mapper;
    }

    public IReadOnlyList<string> Convert(Image image, AsciiConversionSettings settings)
    {
        var width = Mathf.Max(1, image.GetWidth());
        var height = Mathf.Max(1, image.GetHeight());
        var columns = Mathf.Clamp(settings.Columns, 8, 240);
        var rows = Mathf.Max(1, Mathf.RoundToInt((height / (float)width) * columns * settings.CharacterAspect));

        var lines = new List<string>(rows);
        for (var row = 0; row < rows; row++)
        {
            var y0 = row * height / rows;
            var y1 = Mathf.Max(y0 + 1, (row + 1) * height / rows);
            var buffer = new char[columns];

            for (var col = 0; col < columns; col++)
            {
                var x0 = col * width / columns;
                var x1 = Mathf.Max(x0 + 1, (col + 1) * width / columns);
                var luminance = SampleBlockLuminance(image, x0, x1, y0, y1, settings.RespectAlpha);
                luminance = ((luminance - 0.5f) * settings.Contrast) + 0.5f + settings.BrightnessOffset;
                buffer[col] = _mapper.FromLuminance(Mathf.Clamp(luminance, 0f, 1f), settings.Invert);
            }

            lines.Add(new string(buffer));
        }

        return lines;
    }

    private static float SampleBlockLuminance(Image image, int x0, int x1, int y0, int y1, bool respectAlpha)
    {
        float total = 0f;
        var pixels = 0;

        for (var y = y0; y < y1; y++)
        {
            for (var x = x0; x < x1; x++)
            {
                var color = image.GetPixel(x, y);
                var luminance = (0.2126f * color.R) + (0.7152f * color.G) + (0.0722f * color.B);
                if (respectAlpha)
                {
                    luminance = Mathf.Lerp(1f, luminance, color.A);
                }

                total += luminance;
                pixels++;
            }
        }

        return pixels == 0 ? 0f : total / pixels;
    }
}
