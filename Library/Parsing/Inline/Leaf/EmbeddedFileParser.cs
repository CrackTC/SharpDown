using CrackTC.SharpDown.Structure.Inline;
using CrackTC.SharpDown.Structure.Inline.Leaf;

namespace CrackTC.SharpDown.Parsing.Inline.Leaf;

internal class EmbeddedFileParser : IMarkdownLeafInlineParser
{
    public int TryReadAndParse(ReadOnlySpan<char> text, out MarkdownInline? inline)
    {
        inline = null;
        var tmp = text;
        if (!tmp.StartsWith("![[")) return 0;
        tmp = tmp[3..];
        if (!tmp.TryReadUtilUnescaped(']', out var content)) return 0;
        if (!tmp.StartsWith("]]")) return 0;
        tmp = tmp[2..];
        
        inline = new EmbeddedFile(content);
        return text.Length - tmp.Length;
    }
}