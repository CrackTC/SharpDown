using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Leaf;

namespace CrackTC.SharpDown.Parsing.Block.Leaf;

internal class BlankLineParser : IMarkdownBlockParser
{
    public bool TryReadAndParse(ref ReadOnlySpan<char> text, MarkdownBlock father, IEnumerable<IMarkdownBlockParser> blockParsers)
    {
        var line = TextUtils.ReadLine(text, out var remaining, out _, out _);
        if (!line.IsBlankLine()) return false;

        text = remaining;
        father.Children.Add(new BlankLine { Content = line.ToString() });
        return true;
    }
}