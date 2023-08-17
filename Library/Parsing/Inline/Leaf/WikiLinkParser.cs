using CrackTC.SharpDown.Structure.Inline;
using CrackTC.SharpDown.Structure.Inline.Leaf;

namespace CrackTC.SharpDown.Parsing.Inline.Leaf;

internal class WikiLinkParser : IMarkdownLeafInlineParser
{
    public int TryReadAndParse(ReadOnlySpan<char> text, out MarkdownInline? inline)
    {
        inline = null;
        var tmp = text;
        if (!tmp.StartsWith("[[")) return 0;

        tmp = tmp[2..];
        if (!tmp.TryReadUtilUnescaped(']', out var content)) return 0;
        if (!tmp.StartsWith("]]")) return 0;
        tmp = tmp[2..];
        
        var index = content.IndexOf('|');
        inline = index == -1
                 ? new WikiLink(null, content)
                 : new WikiLink(content[(index + 1)..], content[..index]);

        return text.Length - tmp.Length;
    }
}