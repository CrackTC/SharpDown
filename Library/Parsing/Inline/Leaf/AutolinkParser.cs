using System.Text.RegularExpressions;
using CrackTC.SharpDown.Structure.Inline;
using CrackTC.SharpDown.Structure.Inline.Leaf;

namespace CrackTC.SharpDown.Parsing.Inline.Leaf;

internal partial class AutolinkParser : IMarkdownLeafInlineParser
{
    private static bool TryReadUriAutolink(ref ReadOnlySpan<char> text, out Autolink? link)
    {
        link = null;
        if (text.StartsWith("<") is false) return false;

        var tmp = text[1..];
        if (!tmp.TryReadAbsoluteUri(out var uri) || !tmp.StartsWith(">")) return false;
        text = tmp[1..];
        link = new Autolink(uri, new Text(uri));
        return true;
    }


    [GeneratedRegex(@"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$")]
    private static partial Regex EmailRegex();

    private static bool TryReadEmailAutolink(ref ReadOnlySpan<char> text, out Autolink? link)
    {
        link = null;
        if (text.StartsWith("<") is false) return false;

        var endIndex = text.IndexOf('>');
        if (endIndex is -1) return false;

        var emailSpan = text[1..endIndex];
        if (!EmailRegex().IsMatch(emailSpan)) return false;
        text = text[(endIndex + 1)..];
        var email = emailSpan.ToString();
        link = new Autolink("mailto:" + email, new Text(email));
        return true;
    }

    public int TryReadAndParse(ReadOnlySpan<char> text, out MarkdownInline? inline)
    {
        var tmp = text;
        if (TryReadUriAutolink(ref tmp, out var link) || TryReadEmailAutolink(ref tmp, out link))
        {
            inline = link;
            return text.Length - tmp.Length;
        }

        inline = null;
        return 0;
    }
}