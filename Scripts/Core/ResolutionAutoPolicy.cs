using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class ResolutionAutoPolicy
{
    private static readonly Vector2I[] CommonResolutions =
    {
        new(854, 480),
        new(960, 540),
        new(1024, 576),
        new(1152, 648),
        new(1280, 720),
        new(1366, 768),
        new(1440, 900),
        new(1600, 900),
        new(1680, 1050),
        new(1920, 1080),
        new(1920, 1200),
        new(2560, 1440),
        new(2560, 1600),
        new(3440, 1440),
        new(3840, 2160),
    };

    public static IReadOnlyList<ResolutionOption> BuildAvailable(Vector2I maxSize, float monitorAspect)
    {
        var options = new List<ResolutionOption>();
        var seen = new HashSet<long>();

        foreach (var common in CommonResolutions)
        {
            if (common.X > maxSize.X || common.Y > maxSize.Y)
            {
                continue;
            }

            var aspectDiff = Mathf.Abs((common.X / (float)common.Y) - monitorAspect);
            if (aspectDiff > 0.35f)
            {
                continue;
            }

            TryAdd(options, seen, common, isNative: false);
        }

        foreach (var percent in new[] { 100, 95, 90, 85, 80, 75, 70, 66, 60, 50 })
        {
            var w = MakeEven(maxSize.X * percent / 100);
            var h = MakeEven(maxSize.Y * percent / 100);
            TryAdd(options, seen, new Vector2I(w, h), isNative: percent == 100);
        }

        options.Sort((a, b) => a.PixelCount.CompareTo(b.PixelCount));
        return options;
    }

    public static ResolutionOption ChooseAuto(Vector2I maxSize, IReadOnlyList<ResolutionOption> options)
    {
        if (options.Count == 0)
        {
            return ResolutionOption.FromSize(maxSize);
        }

        var targetArea = maxSize.X * maxSize.Y * 0.93f;
        ResolutionOption? bestUnderTarget = null;
        foreach (var option in options)
        {
            if (option.PixelCount <= targetArea)
            {
                bestUnderTarget = option;
            }
        }

        return bestUnderTarget ?? options[^1];
    }

    private static void TryAdd(List<ResolutionOption> options, HashSet<long> seen, Vector2I size, bool isNative)
    {
        if (size.X < 640 || size.Y < 360)
        {
            return;
        }

        var key = ((long)size.X << 32) | (uint)size.Y;
        if (!seen.Add(key))
        {
            return;
        }

        var label = isNative ? $"{size.X}x{size.Y} (Native)" : $"{size.X}x{size.Y}";
        options.Add(new ResolutionOption(size.X, size.Y, label));
    }

    private static int MakeEven(int value)
    {
        return Math.Max(2, (value / 2) * 2);
    }
}
