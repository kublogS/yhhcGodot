using System.Collections.Generic;
using Godot;

public enum ResolutionModePreference
{
    BorderlessFullscreen,
    Windowed,
}

public partial class DisplaySettingsService : Node
{
    public static DisplaySettingsService Instance { get; private set; } = null!;

    [Export] public bool AutoDetectOnStartup = true;
    [Export] public ResolutionModePreference ModePreference = ResolutionModePreference.BorderlessFullscreen;

    private readonly List<ResolutionOption> _available = new();
    private ResolutionOption _current;
    private int _lastScreen = -1;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        if (AutoDetectOnStartup)
        {
            ApplyAutomaticResolution();
        }
    }

    public IReadOnlyList<ResolutionOption> GetAvailableOptions()
    {
        return _available;
    }

    public ResolutionOption GetCurrentOption()
    {
        return _current;
    }

    public void ApplyAutomaticResolution()
    {
        var window = GetWindow();
        var screen = window.CurrentScreen;
        var usableRect = DisplayServer.ScreenGetUsableRect(screen);
        if (usableRect.Size.X <= 0 || usableRect.Size.Y <= 0)
        {
            usableRect = new Rect2I(Vector2I.Zero, DisplayServer.ScreenGetSize(screen));
        }

        var monitorSize = usableRect.Size;
        var monitorAspect = monitorSize.X / (float)Mathf.Max(1, monitorSize.Y);
        _available.Clear();
        _available.AddRange(ResolutionAutoPolicy.BuildAvailable(monitorSize, monitorAspect));
        _current = ResolutionAutoPolicy.ChooseAuto(monitorSize, _available);
        _lastScreen = screen;
        ApplyResolutionInternal(window, usableRect, _current, ModePreference);
    }

    public void ApplyManualResolution(ResolutionOption option, ResolutionModePreference mode)
    {
        var window = GetWindow();
        var screen = window.CurrentScreen;
        var usableRect = DisplayServer.ScreenGetUsableRect(screen);
        if (usableRect.Size.X <= 0 || usableRect.Size.Y <= 0)
        {
            usableRect = new Rect2I(Vector2I.Zero, DisplayServer.ScreenGetSize(screen));
        }

        _current = option;
        _lastScreen = screen;
        ApplyResolutionInternal(window, usableRect, option, mode);
    }

    public override void _Notification(int what)
    {
        if (what != NotificationApplicationFocusIn || !AutoDetectOnStartup)
        {
            return;
        }

        var screen = GetWindow().CurrentScreen;
        if (screen != _lastScreen)
        {
            ApplyAutomaticResolution();
        }
    }

    private static void ApplyResolutionInternal(Window window, Rect2I usableRect, ResolutionOption option, ResolutionModePreference mode)
    {
        window.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        window.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
        window.ContentScaleSize = ChooseUiReferenceSize(option.Size);

        if (mode == ResolutionModePreference.BorderlessFullscreen)
        {
            window.Mode = Window.ModeEnum.Fullscreen;
            return;
        }

        window.Mode = Window.ModeEnum.Windowed;
        window.Size = option.Size;
        window.MinSize = new Vector2I(960, 540);
        var centered = usableRect.Position + ((usableRect.Size - option.Size) / 2);
        window.Position = centered;
    }

    private static Vector2I ChooseUiReferenceSize(Vector2I targetSize)
    {
        if (targetSize.Y >= 1800)
        {
            return new Vector2I(1920, 1080);
        }

        if (targetSize.Y >= 1300)
        {
            return new Vector2I(1600, 900);
        }

        return new Vector2I(1280, 720);
    }
}
