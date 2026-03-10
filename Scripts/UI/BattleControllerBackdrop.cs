using Godot;

public partial class BattleController
{
    private void BindBackdrop()
    {
        // Functional parity with Python: battle keeps continuity with the latest explore frame.
        var contextual = GameSession.Instance.ConsumeBattleBackdrop();
        if (contextual is not null)
        {
            _backdrop.Texture = contextual;
            _backdropFallback.Visible = false;
            return;
        }

        // Fallback keeps battle scene robust when no contextual frame is available.
        _backdrop.Texture = GameAssets.LoadBattleBackground();
        _backdropFallback.Visible = _backdrop.Texture is null;
    }
}
