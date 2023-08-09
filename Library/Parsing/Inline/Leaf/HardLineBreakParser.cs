using CrackTC.SharpDown.Structure.Inline;
using CrackTC.SharpDown.Structure.Inline.Leaf;

namespace CrackTC.SharpDown.Parsing.Inline.Leaf;

internal class HardLineBreakParser : IMarkdownLeafInlineParser
{
    public int TryReadAndParse(ReadOnlySpan<char> text, out MarkdownInline? inline)
    {
        inline = null;
        var spaceCount = text.CountLeadingChracter(ch => ch.IsSpace());

        if (text.StartsWith("\\")) spaceCount = 1;
        else if (spaceCount < 2) return 0;

        if (spaceCount == text.Length) return 0;

        if (text[spaceCount].IsLineEnding())
        {
            _ = TextUtils.ReadLine(text[spaceCount..], out var remaining, out _, out _);
            if (remaining.IsEmpty) return 0;

            var rightSpaceCount = remaining.CountLeadingChracter(ch => ch.IsSpace() || ch.IsTab());
            inline = new HardLineBreak();
            remaining = remaining[rightSpaceCount..];
            return text.Length - remaining.Length;
        }

        return 0;
    }
}