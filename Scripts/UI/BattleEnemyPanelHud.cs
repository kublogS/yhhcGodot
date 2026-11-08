using System;
using System.Collections.Generic;
using Godot;

public sealed class BattleEnemyPanelHud
{
    private const string BasePath = "Root/CombatRow/EnemyPanel/EnemyPanelRoot";
    private readonly GridContainer _enemySlots;
    private readonly List<PanelContainer> _cards = new();
    private readonly List<Label> _nameLabels = new();
    private readonly List<Label> _hpLabels = new();
    private readonly List<ProgressBar> _hpBars = new();
    private readonly List<TextureRect> _sprites = new();
    private readonly List<Button> _rosterButtons = new();
    private readonly StyleBoxFlat _cardStyle;
    private readonly StyleBoxFlat _selectedCardStyle;
    private readonly StyleBoxFlat _rosterStyle;
    private readonly StyleBoxFlat _rosterSelectedStyle;

    public BattleEnemyPanelHud(Control root, Action<int> onSelectTarget)
    {
        _enemySlots = root.GetNode<GridContainer>($"{BasePath}/EnemySlots");
        for (var i = 0; i < 4; i++)
        {
            var index = i + 1;
            var cardPath = $"{BasePath}/EnemySlots/EnemyCard{index}";
            _cards.Add(root.GetNode<PanelContainer>(cardPath));
            _nameLabels.Add(root.GetNode<Label>($"{cardPath}/EnemyCardBox/EnemyName"));
            _hpLabels.Add(root.GetNode<Label>($"{cardPath}/EnemyCardBox/EnemyHpLabel"));
            _hpBars.Add(root.GetNode<ProgressBar>($"{cardPath}/EnemyCardBox/EnemyHpBar"));
            _sprites.Add(root.GetNode<TextureRect>($"{cardPath}/EnemyCardBox/EnemySprite"));

            var rosterButton = root.GetNode<Button>($"{BasePath}/EnemyRoster/EnemyMini{index}");
            var targetIndex = i;
            rosterButton.Pressed += () => onSelectTarget(targetIndex);
            _rosterButtons.Add(rosterButton);
        }

        _cardStyle = BuildCardStyle(new Color(0.12f, 0.16f, 0.23f, 0.84f), new Color(0.74f, 0.81f, 0.93f, 0.68f));
        _selectedCardStyle = BuildCardStyle(new Color(0.16f, 0.19f, 0.28f, 0.94f), new Color(0.97f, 0.84f, 0.41f, 0.9f));
        _rosterStyle = BuildCardStyle(new Color(0.19f, 0.29f, 0.40f, 0.92f), new Color(0.35f, 0.56f, 0.72f, 0.85f));
        _rosterSelectedStyle = BuildCardStyle(new Color(0.20f, 0.35f, 0.24f, 0.95f), new Color(0.58f, 0.78f, 0.42f, 0.9f));
        ApplyStyle();
    }

    public int Refresh(GameState? state, int selectedTarget)
    {
        if (state is null || state.Enemies.Count == 0)
        {
            foreach (var card in _cards)
            {
                card.Visible = false;
            }

            foreach (var button in _rosterButtons)
            {
                button.Visible = false;
                button.Disabled = true;
            }

            return 0;
        }

        var enemyCount = Mathf.Min(4, state.Enemies.Count);
        var clampedTarget = Mathf.Clamp(selectedTarget, 0, enemyCount - 1);
        ApplyLayout(enemyCount);

        for (var i = 0; i < _cards.Count; i++)
        {
            var card = _cards[i];
            var button = _rosterButtons[i];
            if (i >= enemyCount)
            {
                card.Visible = false;
                button.Visible = false;
                button.Disabled = true;
                continue;
            }

            var enemy = state.Enemies[i];
            card.Visible = true;
            card.AddThemeStyleboxOverride("panel", i == clampedTarget ? _selectedCardStyle : _cardStyle);
            _nameLabels[i].Text = enemy.Name;
            _hpLabels[i].Text = $"HP {enemy.Hp}/{enemy.MaxHp}";
            _hpBars[i].MaxValue = Mathf.Max(1, enemy.MaxHp);
            _hpBars[i].Value = Mathf.Clamp(enemy.Hp, 0, enemy.MaxHp);
            _sprites[i].Texture = GameAssets.LoadCharacterSprite(enemy.Sprite, enemy: true);

            button.Visible = true;
            button.Disabled = false;
            button.Text = $"{enemy.Name}\n{enemy.Hp}/{enemy.MaxHp}";
            button.AddThemeStyleboxOverride("normal", i == clampedTarget ? _rosterSelectedStyle : _rosterStyle);
            button.AddThemeStyleboxOverride("hover", i == clampedTarget ? _rosterSelectedStyle : _rosterStyle);
            button.AddThemeStyleboxOverride("pressed", i == clampedTarget ? _rosterSelectedStyle : _rosterStyle);
            button.Modulate = new Color(0.92f, 0.95f, 1f);
        }

        return clampedTarget;
    }

    public void SetEnabled(bool enabled)
    {
        foreach (var button in _rosterButtons)
        {
            button.Disabled = !enabled || !button.Visible;
        }
    }

    private void ApplyLayout(int enemyCount)
    {
        _enemySlots.Columns = enemyCount == 1 ? 1 : 2;
        var spriteHeight = enemyCount switch
        {
            1 => 340f,
            2 => 290f,
            3 => 230f,
            _ => 200f,
        };
        var cardHeight = enemyCount switch
        {
            1 => 420f,
            2 => 360f,
            3 => 300f,
            _ => 270f,
        };

        foreach (var card in _cards)
        {
            card.CustomMinimumSize = new Vector2(0f, cardHeight);
        }

        foreach (var sprite in _sprites)
        {
            sprite.CustomMinimumSize = new Vector2(0f, spriteHeight);
        }
    }

    private void ApplyStyle()
    {
        for (var i = 0; i < _cards.Count; i++)
        {
            _cards[i].AddThemeStyleboxOverride("panel", _cardStyle);
            _nameLabels[i].Modulate = new Color(0.95f, 0.97f, 1f);
            _hpLabels[i].Modulate = new Color(0.84f, 0.96f, 0.9f);
            _sprites[i].ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            _sprites[i].StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            _hpBars[i].ShowPercentage = false;
            _hpBars[i].AddThemeStyleboxOverride("fill", new StyleBoxFlat
            {
                BgColor = new Color(0.86f, 0.2f, 0.26f),
                CornerRadiusTopLeft = 4,
                CornerRadiusTopRight = 4,
                CornerRadiusBottomLeft = 4,
                CornerRadiusBottomRight = 4,
            });
            _hpBars[i].AddThemeStyleboxOverride("background", new StyleBoxFlat
            {
                BgColor = new Color(0.07f, 0.08f, 0.12f, 0.92f),
                CornerRadiusTopLeft = 4,
                CornerRadiusTopRight = 4,
                CornerRadiusBottomLeft = 4,
                CornerRadiusBottomRight = 4,
            });

            _rosterButtons[i].Alignment = HorizontalAlignment.Center;
            _rosterButtons[i].TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        }
    }

    private static StyleBoxFlat BuildCardStyle(Color bg, Color border)
    {
        return new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
        };
    }
}
