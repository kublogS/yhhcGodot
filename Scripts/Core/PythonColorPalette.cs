using Godot;

public static class PythonColorPalette
{
    public static readonly Color Black = Rgb(0x1C, 0x26, 0x38);
    public static readonly Color White = Rgb(0x21, 0x4E, 0x34);
    public static readonly Color GrayDark = Rgb(0x23, 0x49, 0x5D);
    public static readonly Color Gray = Rgb(0x39, 0x79, 0x7A);
    public static readonly Color GrayLight = Rgb(0x95, 0xE0, 0xCC);
    public static readonly Color FloorLightGray = Rgb(210, 210, 210);
    public static readonly Color Red = Rgb(0xE0, 0x39, 0x39);

    public static readonly Color HudBg = Black;
    public static readonly Color PanelBorder = Gray;
    public static readonly Color Text = GrayLight;
    public static readonly Color Muted = Gray;
    public static readonly Color Title = White;
    public static readonly Color BarBg = GrayDark;
    public static readonly Color BarFill = Gray;
    public static readonly Color BarBorder = Gray;
    public static readonly Color BarText = Black;
    public static readonly Color ButtonBg = GrayDark;
    public static readonly Color ButtonBorder = Gray;
    public static readonly Color ButtonText = GrayLight;
    public static readonly Color Danger = Red;

    public static readonly Color Door = Rgb(165, 165, 165);
    public static readonly Color ManualSheet = Rgb(225, 225, 225);
    public static readonly Color SaveSlotTitle = Rgb(194, 24, 91);
    public static readonly Color SaveSlotFill = Rgba(171, 219, 190, 128);

    public static Color OverlayBlack(byte alpha)
    {
        return Rgba(0, 0, 0, alpha);
    }

    public static Color WithAlpha(Color color, byte alpha)
    {
        return new Color(color.R, color.G, color.B, alpha / 255f);
    }

    private static Color Rgb(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }

    private static Color Rgba(int r, int g, int b, int a)
    {
        return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
    }
}
