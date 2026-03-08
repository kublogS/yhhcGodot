using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using FileAccess = Godot.FileAccess;

public partial class ManualOverlayController : Control
{
    private Label _title = null!;
    private RichTextLabel _body = null!;
    private Label _pageLabel = null!;
    private readonly List<(string Title, string Body)> _pages = new();
    private int _pageIndex;

    public override void _Ready()
    {
        _title = GetNode<Label>("Panel/Title");
        _body = GetNode<RichTextLabel>("Panel/Body");
        _pageLabel = GetNode<Label>("Panel/Page");
        Visible = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible)
        {
            return;
        }

        if (@event.IsActionPressed("ui_left"))
        {
            _pageIndex = Math.Max(0, _pageIndex - 1);
            Redraw();
        }
        else if (@event.IsActionPressed("ui_right"))
        {
            _pageIndex = Math.Min(_pages.Count - 1, _pageIndex + 1);
            Redraw();
        }
    }

    public void SetOpen(bool open)
    {
        Visible = open;
        if (open)
        {
            Redraw();
        }
    }

    public void LoadZone(string zone)
    {
        _pages.Clear();
        _pageIndex = 0;

        var userPages = LoadUserPages(zone);
        if (userPages.Count > 0)
        {
            _pages.AddRange(userPages);
        }
        else
        {
            _pages.AddRange(LoadFallbackManual(zone));
        }

        if (_pages.Count == 0)
        {
            _pages.Add(("Manuale", "Manuale non trovato."));
        }

        Redraw();
    }

    private void Redraw()
    {
        if (_pages.Count == 0)
        {
            return;
        }

        _pageIndex = Math.Clamp(_pageIndex, 0, _pages.Count - 1);
        var page = _pages[_pageIndex];
        _title.Text = page.Title;
        _body.Text = page.Body;
        _pageLabel.Text = $"Pagina {_pageIndex + 1}/{_pages.Count}";
    }

    private static List<(string, string)> LoadUserPages(string zone)
    {
        var list = new List<(string, string)>();
        var root = "user://manuals";
        if (!DirAccess.DirExistsAbsolute(root))
        {
            return list;
        }

        using var dir = DirAccess.Open(root);
        if (dir is null)
        {
            return list;
        }

        var prefix = $"codex_zona_{zone.ToUpperInvariant()}_pagina_";
        var files = new List<string>();
        dir.ListDirBegin();
        while (true)
        {
            var name = dir.GetNext();
            if (string.IsNullOrWhiteSpace(name)) break;
            if (!dir.CurrentIsDir() && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                files.Add(name);
            }
        }

        dir.ListDirEnd();
        files.Sort(StringComparer.OrdinalIgnoreCase);
        foreach (var fileName in files)
        {
            using var f = FileAccess.Open($"{root}/{fileName}", FileAccess.ModeFlags.Read);
            list.Add((fileName[..^4], f.GetAsText().Trim()));
        }

        return list;
    }

    private static List<(string, string)> LoadFallbackManual(string zone)
    {
        var output = new List<(string, string)>();
        var path = $"res://Data/manuals/manual_{zone.ToUpperInvariant()}.txt";
        if (!FileAccess.FileExists(path))
        {
            return output;
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        var lines = file.GetAsText().Split('\n');
        string? currentTitle = null;
        var body = new List<string>();

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd('\r');
            if (line.StartsWith("=== TITOLO:", StringComparison.Ordinal) && line.EndsWith("===", StringComparison.Ordinal))
            {
                if (!string.IsNullOrWhiteSpace(currentTitle))
                {
                    output.Add((currentTitle!, string.Join("\n", body).Trim()));
                }

                currentTitle = line[11..^3].Trim();
                body.Clear();
                continue;
            }

            if (!string.IsNullOrWhiteSpace(currentTitle))
            {
                body.Add(line);
            }
        }

        if (!string.IsNullOrWhiteSpace(currentTitle))
        {
            output.Add((currentTitle!, string.Join("\n", body).Trim()));
        }

        return output;
    }
}
