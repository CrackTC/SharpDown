using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Leaf;

namespace CrackTC.SharpDown.Parsing.Block.Leaf;

internal class AtxHeadingParser : IMarkdownBlockParser
{
    private const char AtxHeadingIndicator = '#';
    private static ReadOnlySpan<char> Skip(ReadOnlySpan<char> text, out int level, out string content)
    {
        level = 0;
        content = string.Empty;

        var line = TextUtils.ReadLine(text, out var remaining, out int columnNumber, out _);

        // leading spaces
        var (count, index, _) = line.CountLeadingSpace(columnNumber, 4);
        if (count is 4) return text;
        line = line[index..];

        // level
        level = line.CountLeadingChracter(ch => ch is AtxHeadingIndicator, 7);
        if (level is 0 or 7) return text;
        line = line[level..];
        if (line.IsBlankLine()) return remaining; // blank heading

        // separator spaces
        var spaceCount = line.CountLeadingChracter(ch => ch.IsSpace() || ch.IsTab());
        if (spaceCount is 0) return text;
        line = line[spaceCount..];

        // trailing indicators
        spaceCount = line.CountTrailingChracter(ch => ch.IsSpace() || ch.IsTab());
        while (spaceCount < line.Length && line[^(spaceCount + 1)] is AtxHeadingIndicator) spaceCount++;
        if (spaceCount == line.Length) return remaining; // blank heading

        var ch = line[^(spaceCount + 1)];
        if (!ch.IsSpace() && !ch.IsTab()) // parse trailing indicators as literal
        {
            content = line.ToString();
            return remaining;
        }

        line = line[..^spaceCount];
        spaceCount = line.CountTrailingChracter(ch => ch.IsSpace() || ch.IsTab());
        content = line[..^spaceCount].ToString();
        return remaining;
    }

    public bool TryReadAndParse(ref ReadOnlySpan<char> text, MarkdownBlock father, IEnumerable<IMarkdownBlockParser> blockParser)
    {
        var remaining = Skip(text, out var level, out var content);
        if (remaining == text)
        {
            return false;
        }

        text = remaining;
        father.Children.Add(new AtxHeading(level, content));

        return true;
    }
}
