using CrackTC.SharpDown.Structure.Inline;
using CrackTC.SharpDown.Structure.Inline.Leaf;

namespace CrackTC.SharpDown.Parsing.Inline.Leaf;

internal class HtmlTagParser : IMarkdownLeafInlineParser
{
    public int TryParse(ReadOnlySpan<char> text, out MarkdownInline? inline)
    {
        var tmp = text;
        if (tmp.TryReadOpenTag(out var tag)
            || tmp.TryReadClosingTag(out tag)
            || tmp.TryReadHtmlComment(out tag)
            || tmp.TryReadProcessingInstruction(out tag)
            || tmp.TryReadDeclaration(out tag)
            || tmp.TryReadCdataSection(out tag))
        {
            inline = new HtmlTag(tag);
            return text.Length - tmp.Length;
        }

        inline = null;
        return 0;
    }
}