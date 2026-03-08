using System.Collections.Generic;
using Godot;

public partial class MapOverlayController : Control
{
    private DungeonData? _dungeon;
    private Vector3 _playerPos;
    private readonly List<Vector3> _enemyPos = new();
    private readonly HashSet<Vector2I> _explored = new();
    private RichTextLabel _asciiPreview = null!;

    public override void _Ready()
    {
        _asciiPreview = GetNode<RichTextLabel>("AsciiPanel/AsciiPreview");
        BuildAsciiPreview();
    }

    public void UpdateFromDungeon(DungeonData dungeon, Vector3 playerPos, List<EnemyAgent> enemies)
    {
        _dungeon = dungeon;
        _playerPos = playerPos;
        _enemyPos.Clear();
        foreach (var enemy in enemies)
        {
            _enemyPos.Add(enemy.GlobalPosition);
        }

        var tile = DungeonGenerator.WorldToGrid(playerPos, DungeonBuilder.TileSize);
        for (var y = tile.Y - 3; y <= tile.Y + 3; y++)
        {
            for (var x = tile.X - 3; x <= tile.X + 3; x++)
            {
                _explored.Add(new Vector2I(x, y));
            }
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_dungeon is null)
        {
            return;
        }

        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0, 0, 0, 0.65f), true);
        var mapScale = Mathf.Min((Size.X - 24) / _dungeon.Width, (Size.Y - 24) / _dungeon.Height);
        mapScale = Mathf.Clamp(mapScale, 4f, 24f);
        var offset = new Vector2((Size.X - _dungeon.Width * mapScale) * 0.5f, (Size.Y - _dungeon.Height * mapScale) * 0.5f);

        for (var y = 0; y < _dungeon.Height; y++)
        {
            for (var x = 0; x < _dungeon.Width; x++)
            {
                var tile = _dungeon.GetTile(x, y);
                if (!_explored.Contains(new Vector2I(x, y)))
                {
                    continue;
                }

                var color = tile switch
                {
                    TileType.Wall => new Color(0.22f, 0.22f, 0.22f),
                    TileType.Exit => new Color(0.8f, 0.2f, 0.2f),
                    _ => new Color(0.45f, 0.45f, 0.45f),
                };
                DrawRect(new Rect2(offset + new Vector2(x * mapScale, y * mapScale), new Vector2(mapScale, mapScale)), color, true);
            }
        }

        foreach (var enemy in _enemyPos)
        {
            var e = DungeonGenerator.WorldToGrid(enemy, DungeonBuilder.TileSize);
            DrawCircle(offset + new Vector2((e.X + 0.5f) * mapScale, (e.Y + 0.5f) * mapScale), 3, new Color(1, 0.3f, 0.3f));
        }

        var player = DungeonGenerator.WorldToGrid(_playerPos, DungeonBuilder.TileSize);
        DrawCircle(offset + new Vector2((player.X + 0.5f) * mapScale, (player.Y + 0.5f) * mapScale), 4, new Color(0.9f, 0.9f, 1));
    }

    private void BuildAsciiPreview()
    {
        var imageLoader = new PngImageLoader();
        var image = imageLoader.Load(GameAssets.BattleBackgroundPath);
        if (image is null)
        {
            _asciiPreview.Text = "[code]ASCII preview non disponibile.[/code]";
            return;
        }

        var settings = new AsciiConversionSettings
        {
            Columns = 52,
            CharacterAspect = 0.48f,
            Contrast = 1.1f,
            BrightnessOffset = -0.02f,
        };
        var converter = new AsciiArtConverter(new AsciiLuminanceMapper());
        var lines = converter.Convert(image, settings);
        var formatter = new AsciiArtFormatter();
        _asciiPreview.Text = formatter.ToCodeBlock(lines, trimRight: true);
    }
}
