using System;
using System.Collections.Generic;
using Godot;
using FileAccess = Godot.FileAccess;

public partial class Database : Node
{
    public static Database Instance { get; private set; } = null!;

    private readonly Dictionary<string, string> _itemDescriptions = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<int, MoveModel> _moves = new();
    private TypeSystemConfig _typeSystem = new();

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        LoadAll();
    }

    public void LoadAll()
    {
        _moves = LoadMoves();
        _typeSystem = LoadTypeSystem();
        TypeSystem.SetConfig(_typeSystem);
        LoadItemDescriptions();
    }

    public Dictionary<int, MoveModel> GetMoves()
    {
        return _moves;
    }

    public TypeSystemConfig GetTypeSystem()
    {
        return _typeSystem;
    }

    public string GetItemDescription(string itemId)
    {
        if (_itemDescriptions.TryGetValue(itemId, out var desc) && !string.IsNullOrWhiteSpace(desc))
        {
            return desc;
        }

        var path = $"res://Data/item_desc/{itemId}.txt";
        if (FileAccess.FileExists(path))
        {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            var text = file.GetAsText().Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                _itemDescriptions[itemId] = text;
                return text;
            }
        }

        return "Descrizione non trovata.";
    }

    private Dictionary<int, MoveModel> LoadMoves()
    {
        var path = "res://Data/moves.json";
        if (!FileAccess.FileExists(path))
        {
            GD.PrintErr("moves.json non trovato in res://Data");
            return new Dictionary<int, MoveModel>();
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        return Moves.LoadFromJson(file.GetAsText());
    }

    private TypeSystemConfig LoadTypeSystem()
    {
        var path = "res://Data/type_system.json";
        if (!FileAccess.FileExists(path))
        {
            GD.PrintErr("type_system.json non trovato in res://Data");
            return new TypeSystemConfig();
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        return TypeSystem.BuildConfig(file.GetAsText());
    }

    private void LoadItemDescriptions()
    {
        _itemDescriptions.Clear();
        var dirPath = "res://Data/item_desc";
        if (!DirAccess.DirExistsAbsolute(dirPath))
        {
            return;
        }

        using var dir = DirAccess.Open(dirPath);
        if (dir is null)
        {
            return;
        }

        dir.ListDirBegin();
        while (true)
        {
            var fileName = dir.GetNext();
            if (string.IsNullOrEmpty(fileName))
            {
                break;
            }

            if (dir.CurrentIsDir() || !fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var itemId = fileName[..^4];
            using var f = FileAccess.Open($"{dirPath}/{fileName}", FileAccess.ModeFlags.Read);
            _itemDescriptions[itemId] = f.GetAsText().Trim();
        }

        dir.ListDirEnd();
    }
}
