using CrackTC.SharpDown.Structure.Inline;
using CrackTC.SharpDown.Structure.Inline.Leaf;

namespace CrackTC.SharpDown.Parsing.Inline.Leaf;

internal class SoftLineBreakParser : IMarkdownLeafInlineParser
{
    public int TryReadAndParse(ReadOnlySpan<char> text, out MarkdownInline? inline)
    {
        inline = null;
        if (text.IsEmpty) return 0;
        if (!text[0].IsLineEnding() && (!text[0].IsSpace() || text.Length <= 1 || !text[1].IsLineEnding())) return 0;
        TextUtils.ReadLine(text, out var remaining, out _, out _);
        var rightSpaceCount = remaining.CountLeadingCharacter(ch => ch.IsSpace() || ch.IsTab());
        inline = new SoftLineBreak();
        remaining = remaining[rightSpaceCount..];
        return text.Length - remaining.Length;
    }
}