using CrackTC.SharpDown.Structure.Inline;
using CrackTC.SharpDown.Structure.Inline.Leaf;

namespace CrackTC.SharpDown.Parsing.Inline.Leaf;

internal class WikiLinkParser : IMarkdownLeafInlineParser
{
    public int TryParse(ReadOnlySpan<char> text, out MarkdownInline? inline)
    {
        inline = null;
        var tmp = text;
        if (!tmp.StartsWith("[[")) return 0;
        tmp = tmp[2..];

        var matchCount = 2;
        for (var i = 0; i < tmp.Length; i++)
        {
            var ch = tmp[i];
            switch (ch)
            {
                case '\\':
                    i++;
                    continue;
                case '[':
                    matchCount++;
                    continue;
                case ']' when matchCount == 2:
                {
                    if (!tmp[i..].StartsWith("]]")) return 0;
                    var content = tmp[..i].ToString();
                    tmp = tmp[(i + 2)..];
                    var index = content.IndexOf('|');
                    inline = index == -1
                        ? new WikiLink(null, content)
                        : new WikiLink(content[(index + 1)..], content[..index]);
                    return text.Length - tmp.Length;
                }
                case ']':
                    matchCount--;
                    continue;
            }
        }

        return 0;
    }
}