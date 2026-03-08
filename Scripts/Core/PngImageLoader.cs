using Godot;

public sealed class PngImageLoader
{
    public Image? Load(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        if (!ResourceLoader.Exists(resourcePath))
        {
            return null;
        }

        var texture = GD.Load<Texture2D>(resourcePath);
        if (texture is null)
        {
            return null;
        }

        var image = texture.GetImage();
        return image is null || image.IsEmpty() ? null : image;
    }
}
