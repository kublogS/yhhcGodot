using Godot;

public static class GameUiThemeFactory
{
    private static Theme? _cached;

    public static Theme GetOrCreate()
    {
        _cached ??= BuildTheme();
        return _cached;
    }

    private static Theme BuildTheme()
    {
        var theme = new Theme();

        theme.SetStylebox("panel", "Panel", MakePanelStyle(PythonColorPalette.HudBg, PythonColorPalette.PanelBorder, 1));
        theme.SetStylebox("panel", "PanelContainer", MakePanelStyle(PythonColorPalette.HudBg, PythonColorPalette.PanelBorder, 1));

        theme.SetStylebox("normal", "Button", MakePanelStyle(PythonColorPalette.ButtonBg, PythonColorPalette.ButtonBorder, 1));
        theme.SetStylebox("hover", "Button", MakePanelStyle(PythonColorPalette.ButtonBg, PythonColorPalette.Title, 2));
        theme.SetStylebox("pressed", "Button", MakePanelStyle(PythonColorPalette.Gray, PythonColorPalette.Title, 2));
        theme.SetStylebox("disabled", "Button", MakePanelStyle(PythonColorPalette.GrayDark, PythonColorPalette.PanelBorder, 1));

        theme.SetStylebox("normal", "LineEdit", MakePanelStyle(PythonColorPalette.ButtonBg, PythonColorPalette.ButtonBorder, 1));
        theme.SetStylebox("focus", "LineEdit", MakePanelStyle(PythonColorPalette.ButtonBg, PythonColorPalette.Title, 2));
        theme.SetStylebox("read_only", "LineEdit", MakePanelStyle(PythonColorPalette.GrayDark, PythonColorPalette.PanelBorder, 1));

        theme.SetColor("font_color", "Label", PythonColorPalette.Text);
        theme.SetColor("font_color", "Button", PythonColorPalette.ButtonText);
        theme.SetColor("font_hover_color", "Button", PythonColorPalette.ButtonText);
        theme.SetColor("font_pressed_color", "Button", PythonColorPalette.BarText);
        theme.SetColor("font_disabled_color", "Button", PythonColorPalette.Muted);
        theme.SetColor("font_color", "LineEdit", PythonColorPalette.Text);
        theme.SetColor("font_uneditable_color", "LineEdit", PythonColorPalette.Muted);
        theme.SetColor("selection_color", "LineEdit", PythonColorPalette.WithAlpha(PythonColorPalette.GrayLight, 90));
        theme.SetColor("font_color", "RichTextLabel", PythonColorPalette.Text);
        theme.SetColor("default_color", "RichTextLabel", PythonColorPalette.Text);
        return theme;
    }

    private static StyleBoxFlat MakePanelStyle(Color bg, Color border, int width)
    {
        return new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = border,
            BorderWidthTop = width,
            BorderWidthBottom = width,
            BorderWidthLeft = width,
            BorderWidthRight = width,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 8,
            ContentMarginTop = 8,
            ContentMarginRight = 8,
            ContentMarginBottom = 8,
        };
    }
}
