using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Godot;
using FileAccess = Godot.FileAccess;

public static class ManualTextRepository
{
    private const string HeaderPrefix = "=== TITOLO:";
    private const string PythonRepoEnv = "YHHC_PY_REPO";

    public static List<(string Title, string Body)> LoadZonePages(string zoneName)
    {
        var zone = string.IsNullOrWhiteSpace(zoneName) ? "INDICE" : zoneName.Trim().ToUpperInvariant();
        var raw = TryReadFromPythonRepo(zone) ?? TryReadBundledManual(zone);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<(string, string)> { ("Manuale", "Manuale non trovato.") };
        }

        var pages = ParsePages(raw);
        return pages.Count > 0 ? pages : new List<(string, string)> { ("Manuale", "Manuale non trovato.") };
    }

    public static List<(string Title, string Body)> ParsePages(string raw)
    {
        var pages = new List<(string, string)>();
        string? currentTitle = null;
        var bodyLines = new List<string>();

        foreach (var line in raw.Split('\n'))
        {
            var clean = line.TrimEnd('\r');
            var title = ParseHeader(clean);
            if (title is not null)
            {
                if (!string.IsNullOrWhiteSpace(currentTitle))
                {
                    pages.Add((currentTitle!, NormalizeBody(bodyLines)));
                }

                currentTitle = title;
                bodyLines.Clear();
                continue;
            }

            if (!string.IsNullOrWhiteSpace(currentTitle))
            {
                bodyLines.Add(clean);
            }
        }

        if (!string.IsNullOrWhiteSpace(currentTitle))
        {
            pages.Add((currentTitle!, NormalizeBody(bodyLines)));
        }

        pages.RemoveAll(page => string.IsNullOrWhiteSpace(page.Item1));
        return pages;
    }

    public static string BuildExcerpt(string body, int maxLines = 6, int maxLineLength = 34)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "...";
        }

        var lines = new List<string>();
        foreach (var rawLine in body.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            while (line.Length > maxLineLength)
            {
                lines.Add(line[..maxLineLength].TrimEnd());
                line = line[maxLineLength..].TrimStart();
                if (lines.Count >= maxLines)
                {
                    break;
                }
            }

            if (lines.Count >= maxLines)
            {
                break;
            }

            if (line.Length > 0)
            {
                lines.Add(line);
            }

            if (lines.Count >= maxLines)
            {
                break;
            }
        }

        if (lines.Count == 0)
        {
            return "...";
        }

        if (lines.Count >= maxLines)
        {
            lines[^1] = lines[^1].TrimEnd('.', ' ') + "...";
        }

        return string.Join("\n", lines);
    }

    private static string? TryReadFromPythonRepo(string zone)
    {
        foreach (var path in EnumeratePythonCandidates(zone))
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                return File.ReadAllText(path, Encoding.UTF8);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static string? TryReadBundledManual(string zone)
    {
        var path = $"res://Data/manuals/manual_{zone}.txt";
        if (!FileAccess.FileExists(path))
        {
            return null;
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        return file.GetAsText();
    }

    private static IEnumerable<string> EnumeratePythonCandidates(string zone)
    {
        var fileName = $"manual_{zone}.txt";
        var fromEnv = System.Environment.GetEnvironmentVariable(PythonRepoEnv);
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            yield return Path.Combine(fromEnv, "data", "manuals", fileName);
        }

        var projectRoot = ProjectSettings.GlobalizePath("res://");
        var parent = Directory.GetParent(projectRoot)?.FullName;
        if (!string.IsNullOrWhiteSpace(parent))
        {
            yield return Path.Combine(parent, "YouhavetohaveCharacter", "data", "manuals", fileName);
        }

        var documents = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
        if (!string.IsNullOrWhiteSpace(documents))
        {
            yield return Path.Combine(documents, "GitHub", "YouhavetohaveCharacter", "data", "manuals", fileName);
        }
    }

    private static string? ParseHeader(string line)
    {
        var trimmed = line.Trim();
        if (!trimmed.StartsWith(HeaderPrefix, StringComparison.Ordinal) || !trimmed.EndsWith("===", StringComparison.Ordinal))
        {
            return null;
        }

        var title = trimmed[HeaderPrefix.Length..^3].Trim();
        return title.Length == 0 ? "Pagina" : title;
    }

    private static string NormalizeBody(List<string> lines)
    {
        var body = string.Join("\n", lines).Trim('\n', '\r').Trim();
        return body.Length == 0 ? " " : body;
    }
}
