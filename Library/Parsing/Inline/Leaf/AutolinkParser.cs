using System.Text.RegularExpressions;
using CrackTC.SharpDown.Parsing;
using CrackTC.SharpDown.Parsing.Inline.Leaf;
using CrackTC.SharpDown.Structure.Inline;
using CrackTC.SharpDown.Structure.Inline.Leaf;

namespace CrackTC.SharpDown.Core.Parsing.Inline.Leaf;

internal partial class AutolinkParser : IMarkdownLeafInlineParser
{
    private static bool TryReadUriAutolink(ref ReadOnlySpan<char> text, out Autolink? link)
    {
        if (text.StartsWith("<") is false)
        {
            link = null;
            return false;
        }

        var tmp = text[1..];
        if (tmp.TryReadAbsoluteUri(out var uri) && tmp.StartsWith(">"))
        {
            text = tmp[1..];
            link = new(uri, new(uri));
            return true;
        }

        link = null;
        return false;
    }


    [GeneratedRegex(@"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$")]
    private static partial Regex EmailRegex();

    private static bool TryReadEmailAutolink(ref ReadOnlySpan<char> text, out Autolink? link)
    {
        if (text.StartsWith("<") is false)
        {
            link = null;
            return false;
        }

        int endIndex = text.IndexOf('>');
        if (endIndex is -1)
        {
            link = null;
            return false;
        }

        var emailSpan = text[1..endIndex];
        if (EmailRegex().IsMatch(emailSpan))
        {
            text = text[(endIndex + 1)..];
            var email = emailSpan.ToString();
            link = new("mailto:" + email, new(email));
            return true;
        }

        link = null;
        return false;
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