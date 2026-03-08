using System.Collections.Generic;
using System.Text;

public sealed class AsciiArtFormatter
{
    public string ToMultiline(IReadOnlyList<string> lines, bool trimRight = false)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < lines.Count; i++)
        {
            var line = trimRight ? lines[i].TrimEnd() : lines[i];
            builder.Append(line);
            if (i < lines.Count - 1)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }

    public string ToCodeBlock(IReadOnlyList<string> lines, bool trimRight = false)
    {
        var content = ToMultiline(lines, trimRight);
        return $"[code]{content}[/code]";
    }
}
