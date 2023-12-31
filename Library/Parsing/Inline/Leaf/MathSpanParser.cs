using CrackTC.SharpDown.Structure.Inline;
using CrackTC.SharpDown.Structure.Inline.Leaf;

namespace CrackTC.SharpDown.Parsing.Inline.Leaf;

internal class MathSpanParser : IMarkdownLeafInlineParser
{
    public int TryParse(ReadOnlySpan<char> text, out MarkdownInline? inline)
    {
        var result = TryParseBlock(text, out inline);
        return result != 0 ? result : TryParseInline(text, out inline);
    }

    private static int TryParseInline(ReadOnlySpan<char> text, out MarkdownInline? inline)
    {
        inline = null;

        var tmp = text;
        if (!tmp.StartsWith("$")) return 0;
        tmp = tmp[1..];
        if (!tmp.TryReadUtilUnescaped('$', out var content)) return 0;
        tmp = tmp[1..];
        inline = new MathSpan($"${content}$");
        return text.Length - tmp.Length;
    }

    private static int TryParseBlock(ReadOnlySpan<char> text, out MarkdownInline? inline)
    {
        inline = null;

        var tmp = text;
        if (!tmp.StartsWith("$$")) return 0;
        tmp = tmp[2..];
        if (!tmp.TryReadUtilUnescaped('$', out var content)) return 0;
        if (!tmp.StartsWith("$$")) return 0;
        tmp = tmp[2..];
        inline = new MathSpan($"$${content}$$");
        return text.Length - tmp.Length;
    }
}