using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Leaf;

namespace CrackTC.SharpDown.Parsing.Block.Leaf;

internal class ThematicBreakParser : IMarkdownBlockParser
{
    private const string ValidChars = "-_*";
    private static ReadOnlySpan<char> Skip(ReadOnlySpan<char> text)
    {
        char? validChar = null;
        var line = TextUtils.ReadLine(text, out var remaining, out var columnNumber, out _);

        var (count, index, _) = line.CountLeadingSpace(columnNumber, 4);
        if (count is 4) return text;

        int validCount = 0;
        while (index < line.Length)
        {
            char ch = line[index];
            index++;
            if (validChar is null && ValidChars.Contains(ch))
            {
                validChar = ch;
                validCount++;
            }
            else if (validChar == ch) validCount++;
            else if (!ch.IsSpace() && !ch.IsTab()) return text;
        }

        return validCount >= 3 ? remaining : text;
    }

    public bool TryReadAndParse(ref ReadOnlySpan<char> text, MarkdownBlock father, IEnumerable<IMarkdownBlockParser> blockParsers)
    {
        var remaining = Skip(text);
        if (remaining == text) return false;

        text = remaining;
        father.Children.Add(new ThematicBreak());

        return true;
    }
}
