using Godot;
using System;

public partial class PauseOverlayController : Control
{
    private DisplaySettingsService _displaySettings = null!;
    private VBoxContainer _settingsBox = null!;
    private Label _settingsInfo = null!;

    public event Action? ResumeRequested;
    public event Action? ReturnToIndexRequested;
    public event Action? MainMenuRequested;

    public override void _Ready()
    {
        _displaySettings = GetNode<DisplaySettingsService>("/root/DisplaySettings");
        var resume = GetNode<Button>("Panel/VBox/Resume");
        var toIndex = GetNode<Button>("Panel/VBox/ToIndex");
        var settings = GetNode<Button>("Panel/VBox/Settings");
        var toMain = GetNode<Button>("Panel/VBox/ToMainMenu");
        _settingsBox = GetNode<VBoxContainer>("Panel/VBox/SettingsBox");
        _settingsInfo = GetNode<Label>("Panel/VBox/SettingsBox/Info");

        resume.Pressed += () =>
        {
            SetOverlayVisible(false);
            ResumeRequested?.Invoke();
        };
        toIndex.Pressed += () => ReturnToIndexRequested?.Invoke();
        toMain.Pressed += () => MainMenuRequested?.Invoke();
        settings.Pressed += ToggleSettings;
        GetNode<Button>("Panel/VBox/SettingsBox/Auto").Pressed += ApplyAutoSettings;
        GetNode<Button>("Panel/VBox/SettingsBox/ToggleMode").Pressed += ToggleWindowMode;
        GetNode<Button>("Panel/VBox/SettingsBox/Close").Pressed += ToggleSettings;

        _settingsBox.Visible = false;
        Visible = false;
    }

    public void SetOverlayVisible(bool open)
    {
        Visible = open;
        if (!open)
        {
            _settingsBox.Visible = false;
        }
        else
        {
            RefreshSettingsInfo();
        }
    }

    private void ToggleSettings()
    {
        _settingsBox.Visible = !_settingsBox.Visible;
        if (_settingsBox.Visible)
        {
            RefreshSettingsInfo();
        }
    }

    private void ApplyAutoSettings()
    {
        _displaySettings.ApplyAutomaticResolution();
        RefreshSettingsInfo();
    }

    private void ToggleWindowMode()
    {
        var current = _displaySettings.GetCurrentOption();
        var isFullscreen = GetWindow().Mode == Window.ModeEnum.Fullscreen;
        var mode = isFullscreen ? ResolutionModePreference.Windowed : ResolutionModePreference.BorderlessFullscreen;
        _displaySettings.ApplyManualResolution(current, mode);
        RefreshSettingsInfo();
    }

    private void RefreshSettingsInfo()
    {
        var option = _displaySettings.GetCurrentOption();
        var mode = GetWindow().Mode == Window.ModeEnum.Fullscreen ? "Fullscreen" : "Windowed";
        _settingsInfo.Text = $"Risoluzione: {option.Label}  •  Modalita: {mode}";
    }
}
