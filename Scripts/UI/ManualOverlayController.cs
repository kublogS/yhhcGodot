using System;
using System.Collections.Generic;
using Godot;

public partial class ManualOverlayController : Control
{
    private Label _title = null!;
    private RichTextLabel _body = null!;
    private Label _pageLabel = null!;
    private readonly List<(string Title, string Body)> _pages = new();
    private int _pageIndex;

    public override void _Ready()
    {
        Theme = GameUiThemeFactory.GetOrCreate();
        _title = GetNode<Label>("Panel/Title");
        _body = GetNode<RichTextLabel>("Panel/Body");
        _pageLabel = GetNode<Label>("Panel/Page");

        _title.AddThemeFontSizeOverride("font_size", 26);
        _body.AddThemeFontSizeOverride("normal_font_size", 19);
        _body.AddThemeConstantOverride("line_separation", 6);
        _body.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _body.ScrollActive = true;
        _body.SelectionEnabled = true;
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
        _pages.AddRange(ManualTextRepository.LoadZonePages(zone));
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
        _pageLabel.Text = $"Pagina {_pageIndex + 1}/{_pages.Count}  •  ←/→ per cambiare";
    }
}
