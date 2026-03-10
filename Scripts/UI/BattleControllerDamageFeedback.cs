using Godot;

public partial class BattleController
{
    private Tween? _playerHitTween;
    private Tween? _enemyHitTween;
    private readonly Tween?[] _targetHitTweens = new Tween?[4];

    private void ConfigureDamageFeedback()
    {
        _playerHitFlash.Visible = false;
        _playerHitFlash.MouseFilter = Control.MouseFilterEnum.Ignore;
        _playerHitFlash.Color = new Color(0.92f, 0.08f, 0.08f, 0f);

        _enemyHitFlash.Visible = false;
        _enemyHitFlash.MouseFilter = Control.MouseFilterEnum.Ignore;
        _enemyHitFlash.Color = new Color(0.92f, 0.08f, 0.08f, 0f);
    }

    private void TriggerDamageFeedback(CombatEventEntry battleEvent)
    {
        if (battleEvent.EventType != CombatEventType.DamageDealt || battleEvent.Amount.GetValueOrDefault() <= 0)
        {
            return;
        }

        if (battleEvent.TargetId == "player")
        {
            FlashPlayerHit();
            return;
        }

        if (battleEvent.TargetSlot.HasValue)
        {
            FlashTargetSlot(battleEvent.TargetSlot.Value);
        }

        if (battleEvent.TargetSlot == _selectedTarget || (!battleEvent.TargetSlot.HasValue && battleEvent.TargetId?.StartsWith("enemy:") == true))
        {
            FlashEnemyHit();
        }
    }

    private void FlashPlayerHit()
    {
        _playerHitTween?.Kill();
        _playerHitFlash.Visible = true;
        _playerHitFlash.Color = new Color(0.92f, 0.08f, 0.08f, 0f);
        _playerHitTween = CreateTween();
        _playerHitTween.TweenProperty(_playerHitFlash, "color:a", 0.42f, 0.05f);
        _playerHitTween.TweenProperty(_playerHitFlash, "color:a", 0f, 0.24f);
        _playerHitTween.Finished += () => _playerHitFlash.Visible = false;
    }

    private void FlashEnemyHit()
    {
        _enemyHitTween?.Kill();
        _enemyHitFlash.Visible = true;
        _enemyHitFlash.Color = new Color(0.92f, 0.08f, 0.08f, 0f);
        _enemyHitTween = CreateTween();
        _enemyHitTween.TweenProperty(_enemyHitFlash, "color:a", 0.5f, 0.05f);
        _enemyHitTween.TweenProperty(_enemyHitFlash, "color:a", 0f, 0.22f);
        _enemyHitTween.Finished += () => _enemyHitFlash.Visible = false;
    }

    private void FlashTargetSlot(int targetSlot)
    {
        if (targetSlot < 0 || targetSlot >= _targetButtons.Count)
        {
            return;
        }

        var button = _targetButtons[targetSlot];
        if (!button.Visible)
        {
            return;
        }

        _targetHitTweens[targetSlot]?.Kill();
        button.SelfModulate = Colors.White;
        var tween = CreateTween();
        _targetHitTweens[targetSlot] = tween;
        tween.TweenProperty(button, "self_modulate", new Color(1f, 0.35f, 0.35f, 1f), 0.05f);
        tween.TweenProperty(button, "self_modulate", Colors.White, 0.2f);
    }
}
