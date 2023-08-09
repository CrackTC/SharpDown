using CrackTC.SharpDown.Structure.Inline;

namespace CrackTC.SharpDown.Parsing.Inline.Leaf;

internal interface IMarkdownLeafInlineParser
{
    public int TryReadAndParse(ReadOnlySpan<char> text, out MarkdownInline? inline);
}
