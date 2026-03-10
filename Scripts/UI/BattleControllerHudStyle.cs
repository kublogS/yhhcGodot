using Godot;

public partial class BattleController
{
    private static Texture2D? _cachedPaperTexture;

    private void ConfigureHudPresentation()
    {
        ApplyHudPaperStyle();

        _paperGrain.Texture = GetPaperTexture();
        _paperGrain.TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled;
        _paperGrain.StretchMode = TextureRect.StretchModeEnum.Tile;
        _paperGrain.Modulate = new Color(1f, 1f, 1f, 0.26f);
        _glossBand.Color = new Color(1f, 0.98f, 0.92f, 0.16f);

        var readableText = new Color(0.09f, 0.12f, 0.08f, 1f);
        _log.AddThemeColorOverride("default_color", readableText);
        _log.AddThemeColorOverride("font_color", readableText);
        foreach (var button in AllInteractiveButtons())
        {
            button.AddThemeColorOverride("font_color", readableText);
            button.AddThemeColorOverride("font_hover_color", readableText);
            button.AddThemeColorOverride("font_pressed_color", readableText);
        }
    }

    private void ApplyHudPaperStyle()
    {
        var hudStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.86f, 0.79f, 0.65f, 0.64f),
            BorderColor = new Color(0.45f, 0.35f, 0.2f, 0.9f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ContentMarginLeft = 10,
            ContentMarginTop = 10,
            ContentMarginRight = 10,
            ContentMarginBottom = 10,
            ShadowColor = new Color(0f, 0f, 0f, 0.18f),
            ShadowSize = 4,
        };
        _hudPanel.AddThemeStyleboxOverride("panel", hudStyle);
    }

    private static Texture2D GetPaperTexture()
    {
        if (_cachedPaperTexture is not null)
        {
            return _cachedPaperTexture;
        }

        const int width = 192;
        const int height = 192;
        var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        var rng = new RandomNumberGenerator();
        rng.Seed = 20260310;

        for (var y = 0; y < height; y++)
        {
            var v = y / (float)(height - 1);
            var topSheen = Mathf.Max(0f, 0.12f - Mathf.Abs(v - 0.16f) * 0.6f);
            for (var x = 0; x < width; x++)
            {
                var grain = rng.RandfRange(-0.05f, 0.05f);
                var warm = Mathf.Sin((x * 0.16f) + (y * 0.12f)) * 0.015f;
                var r = Mathf.Clamp(0.90f + grain + warm, 0f, 1f);
                var g = Mathf.Clamp(0.84f + grain * 0.7f, 0f, 1f);
                var b = Mathf.Clamp(0.72f + grain * 0.35f - warm * 0.6f, 0f, 1f);
                var a = Mathf.Clamp(0.34f + topSheen, 0f, 0.58f);
                image.SetPixel(x, y, new Color(r, g, b, a));
            }
        }

        _cachedPaperTexture = ImageTexture.CreateFromImage(image);
        return _cachedPaperTexture;
    }
}
