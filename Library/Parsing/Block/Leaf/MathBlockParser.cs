using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Leaf;

namespace CrackTC.SharpDown.Parsing.Block.Leaf;

internal class MathBlockParser : IMarkdownBlockParser
{
    public bool TryReadAndParse(ref ReadOnlySpan<char> text, MarkdownBlock father,
        IEnumerable<IMarkdownBlockParser> parsers)
    {
        var tmp = text;
        var line = TextUtils.ReadLine(tmp, out var remaining, out var columnNumber, out _);
        var (count, index, _) = line.CountLeadingSpace(columnNumber, 4);
        if (count == 4) return false;

        tmp = tmp[index..];
        if (!tmp.StartsWith("$$")) return false;
        tmp = tmp[2..];
        if (!tmp.TryReadUtilUnescaped('$', out var content)) return false;
        if (!tmp.StartsWith("$$")) return false;
        tmp = tmp[2..];
        text = tmp;

        father.Children.Add(new MathBlock($"$${content}$$"));
        return true;
    }
}
