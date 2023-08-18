using CrackTC.SharpDown.Structure.Block;

namespace CrackTC.SharpDown.Parsing.Block;

internal interface IMarkdownBlockParser
{
    bool TryReadAndParse(ref ReadOnlySpan<char> text,
        MarkdownBlock father,
        IEnumerable<IMarkdownBlockParser> parsers);
}