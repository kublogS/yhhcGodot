using System;
using System.Collections.Generic;
using Godot;

public static class GameAssets
{
    public const string AppIconPath = "res://Assets/icons/icon.png";
    public const string MainMenuWallpaperPath = "res://Assets/ui/startmenuwallpaper.png";
    public const string BattleBackgroundPath = "res://Assets/backgrounds/room.png";
    public const string BattleBackgroundSourcePath = "res://Assets/backgrounds/room.pdf";
    public const string PlayerSpritePath = "res://Assets/characters/player/player.png";
    public const string EnemyFallbackSpritePath = "res://Assets/characters/enemy/enemy.png";

    private static readonly Dictionary<string, string> EnemySpritePaths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["enemy"] = EnemyFallbackSpritePath,
        ["goblin"] = "res://Assets/characters/enemy/goblin.png",
        ["orco"] = "res://Assets/characters/enemy/orco.png",
        ["scheletro"] = "res://Assets/characters/enemy/scheletro.png",
        ["slime"] = "res://Assets/characters/enemy/slime.png",
        ["bandito"] = "res://Assets/characters/enemy/bandito.png",
        ["stregone"] = "res://Assets/characters/enemy/stregone.png",
        ["linussss"] = "res://Assets/characters/enemy/linussss.png",
    };

    public static Texture2D? LoadAppIcon()
    {
        return LoadTexture(AppIconPath);
    }

    public static Texture2D? LoadMainMenuWallpaper()
    {
        return LoadTexture(MainMenuWallpaperPath);
    }

    public static Texture2D? LoadBattleBackground()
    {
        return LoadTexture(BattleBackgroundPath);
    }

    public static Texture2D? LoadPlayerSprite()
    {
        return LoadTexture(PlayerSpritePath) ?? LoadEnemySprite("enemy");
    }

    public static Texture2D? LoadCharacterSprite(string? spriteId, bool enemy = false)
    {
        if (enemy)
        {
            return LoadEnemySprite(spriteId);
        }

        if (!string.IsNullOrWhiteSpace(spriteId) && !string.Equals(spriteId, "player", StringComparison.OrdinalIgnoreCase))
        {
            return LoadEnemySprite(spriteId);
        }

        return LoadPlayerSprite();
    }

    public static string ResolveEnemySpritePath(string? spriteId)
    {
        var key = string.IsNullOrWhiteSpace(spriteId) ? "enemy" : spriteId.Trim();
        if (EnemySpritePaths.TryGetValue(key, out var knownPath) && ResourceLoader.Exists(knownPath))
        {
            return knownPath;
        }

        var dynamicPath = $"res://Assets/characters/enemy/{key}.png";
        return ResourceLoader.Exists(dynamicPath) ? dynamicPath : EnemyFallbackSpritePath;
    }

    public static Texture2D? LoadEnemySprite(string? spriteId)
    {
        var dynamicTexture = LoadTexture(ResolveEnemySpritePath(spriteId));
        if (dynamicTexture is not null)
        {
            return dynamicTexture;
        }

        return LoadTexture(EnemyFallbackSpritePath);
    }

    private static Texture2D? LoadTexture(string path)
    {
        if (!ResourceLoader.Exists(path))
        {
            return null;
        }

        return GD.Load<Texture2D>(path);
    }
}
