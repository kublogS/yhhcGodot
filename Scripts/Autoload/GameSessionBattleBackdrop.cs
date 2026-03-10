using Godot;

public partial class GameSession
{
    private Texture2D? _pendingBattleBackdrop;
    public string PendingBattleBackdropSource { get; private set; } = "none";

    public bool TryCaptureBattleBackdrop(Viewport? viewport, string source = "unknown")
    {
        if (viewport is null)
        {
            ClearBattleBackdrop();
            return false;
        }

        var viewportTexture = viewport.GetTexture();
        if (viewportTexture is null)
        {
            ClearBattleBackdrop();
            return false;
        }

        var image = viewportTexture.GetImage();
        if (image is null || image.IsEmpty())
        {
            ClearBattleBackdrop();
            return false;
        }

        _pendingBattleBackdrop = ImageTexture.CreateFromImage(image);
        PendingBattleBackdropSource = string.IsNullOrWhiteSpace(source) ? "unknown" : source;
        return true;
    }

    public Texture2D? ConsumeBattleBackdrop()
    {
        var backdrop = _pendingBattleBackdrop;
        _pendingBattleBackdrop = null;
        PendingBattleBackdropSource = "none";
        return backdrop;
    }

    public void ClearBattleBackdrop()
    {
        _pendingBattleBackdrop = null;
        PendingBattleBackdropSource = "none";
    }
}
