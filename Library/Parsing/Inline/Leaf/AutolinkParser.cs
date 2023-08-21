using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using CrackTC.SharpDown.Structure.Inline;
using CrackTC.SharpDown.Structure.Inline.Leaf;

namespace CrackTC.SharpDown.Parsing.Inline.Leaf;

internal partial class AutolinkParser : IMarkdownLeafInlineParser
{
    public int TryParse(ReadOnlySpan<char> text, out MarkdownInline? inline)
    {
        var tmp = text;
        if (TryReadUriAutolink(ref tmp, out var link)
            || TryReadEmailAutolink(ref tmp, out link)
            || TryReadExtendedWwwAutolink(ref tmp, out link)
            || TryReadExtendedUrlAutolink(ref tmp, out link)
            || TryReadExtendedEmailAutolink(ref tmp, out link)
            || TryReadExtendedProtocolAutolink(ref tmp, out link))
        {
            inline = link;
            return text.Length - tmp.Length;
        }

        inline = null;
        return 0;
    }

    private static bool TryReadUriAutolink(ref ReadOnlySpan<char> text, [NotNullWhen(true)] out Autolink? link)
    {
        link = null;
        if (text.StartsWith("<") is false) return false;

        var tmp = text[1..];
        if (!tmp.TryReadAbsoluteUri(out var uri) || !tmp.StartsWith(">")) return false;
        text = tmp[1..];
        link = new Autolink(uri, new Text(uri));
        return true;
    }


    [GeneratedRegex(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$")]
    private static partial Regex EmailRegex();

    private static bool TryReadEmailAutolink(ref ReadOnlySpan<char> text, [NotNullWhen(true)] out Autolink? link)
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

    [GeneratedRegex(
        @"^www(?:\.([-_a-zA-Z0-9]+)){2,}([^<\s]*)")]
    private static partial Regex ExtendedWwwAutolinkRegex();

    private static void RemoveTrailingPunctuation(ref ReadOnlySpan<char> destination)
    {
        while (destination[^1].IsUnicodePunctuation())
            destination = destination[..^1];
    }

    private static void RemoveClosingParentheses(ref ReadOnlySpan<char> destination)
    {
        var index = destination.Length - 1;
        while (destination[index] == ')') index--;
        var matchCount = 0;
        for (var i = 0; i <= index; i++)
            matchCount += destination[i] switch
            {
                '(' => 1,
                ')' => -1,
                _ => 0
            };

        if (matchCount < 0) matchCount = 0;
        index += matchCount + 1;
        if (index > destination.Length) index = destination.Length;
        destination = destination[..index];
    }

    private static void RemoveTrailingEntities(ref ReadOnlySpan<char> destination)
    {
        while (destination.EndsWith(";"))
        {
            var index = destination.LastIndexOf('&');
            var maybeEntity = true;
            for (var i = index + 1; i < destination.Length; i++)
            {
                var ch = destination[i];
                if (char.IsAsciiDigit(ch) || char.IsAsciiLetter(ch)) continue;
                maybeEntity = false;
                break;
            }

            if (!maybeEntity) break;
            destination = destination[..index];
        }
    }

    private static void ValidateExtendedAutolinkPath(ref ReadOnlySpan<char> destination)
    {
        RemoveTrailingPunctuation(ref destination);
        RemoveClosingParentheses(ref destination);
        RemoveTrailingEntities(ref destination);
    }

    private static bool IsValidDomainSegments(List<string> segments)
    {
        return !segments[^1].Contains('_') && (segments.Count <= 1 || !segments[^2].Contains('_'));
    }

    private static bool TryReadExtendedWwwAutolink(ref ReadOnlySpan<char> text, [NotNullWhen(true)] out Autolink? link)
    {
        link = null;
        var line = TextUtils.ReadLine(text, out _, out _, out _);
        var match = ExtendedWwwAutolinkRegex().Match(line.ToString());
        if (!match.Success) return false;
        if (!IsValidDomainSegments(match.Groups[1].Captures.Select(c => c.Value).ToList())) return false;

        var destinationSpan = match.ValueSpan;

        ValidateExtendedAutolinkPath(ref destinationSpan);

        text = text[destinationSpan.Length..];
        var destination = destinationSpan.ToString();
        link = new Autolink("http://" + destination, new Text(destination));

        return true;
    }

    [GeneratedRegex(
        @"^(?:http://|https://)([-_a-zA-Z0-9]+)(?:\.([-_a-zA-Z0-9]+))+([^<\s]*)")]
    private static partial Regex ExtendedUrlAutolinkRegex();

    private static bool TryReadExtendedUrlAutolink(ref ReadOnlySpan<char> text, [NotNullWhen(true)] out Autolink? link)
    {
        link = null;
        var line = TextUtils.ReadLine(text, out _, out _, out _);
        var match = ExtendedUrlAutolinkRegex().Match(line.ToString());
        if (!match.Success) return false;
        if (!IsValidDomainSegments(
                match.Groups[2].Captures.Select(c => c.Value).Prepend(match.Groups[1].Value).ToList())) return false;

        var destinationSpan = match.ValueSpan;

        ValidateExtendedAutolinkPath(ref destinationSpan);

        text = text[destinationSpan.Length..];
        var destination = destinationSpan.ToString();
        link = new Autolink(destination, new Text(destination));

        return true;
    }

    [GeneratedRegex(
        @"^[a-zA-Z0-9-._+]+@[a-zA-Z0-9-_]+(?:\.[a-zA-Z0-9-_]+)+")]
    private static partial Regex ExtendedEmailAutolinkRegex();

    private static bool TryReadExtendedEmailAutolink(ref ReadOnlySpan<char> text,
        [NotNullWhen(true)] out Autolink? link)
    {
        link = null;
        var line = TextUtils.ReadLine(text, out _, out _, out _);
        var match = ExtendedEmailAutolinkRegex().Match(line.ToString());
        if (!match.Success) return false;
        if ("-_".Contains(match.ValueSpan[^1])) return false;

        var destination = match.Value;
        text = text[destination.Length..];
        link = new Autolink("mailto:" + destination, new Text(destination));
        return true;
    }

    private static bool TryReadExtendedProtocolAutolink(ref ReadOnlySpan<char> text,
        [NotNullWhen(true)] out Autolink? link)
    {
        link = null;
        if (!text.StartsWith("mailto:") && !text.StartsWith("xmpp:")) return false;
        var line = TextUtils.ReadLine(text, out _, out _, out _);

        if (text.StartsWith("mailto:"))
        {
            line = line["mailto:".Length..];
            if (!TryReadExtendedEmailAutolink(ref line, out link)) return false;
            text = text[link.Destination.Length..];
            return true;
        }

        // ReSharper disable once InvertIf
        if (text.StartsWith("xmpp:"))
        {
            line = line["xmpp:".Length..];
            var match = ExtendedEmailAutolinkRegex().Match(line.ToString());
            if (!match.Success) return false;
            if ("-_".Contains(match.ValueSpan[^1])) return false;

            var destination = match.Value;
            line = line[destination.Length..];

            var i = 1;
            if (line.StartsWith("/"))
                for (; i < line.Length; i++)
                    if (!char.IsAsciiLetter(line[i]) && !char.IsAsciiDigit(line[i]) && line[i] != '@' &&
                        line[i] != '.')
                        break;

            destination += line[..i].ToString();
            link = new Autolink("xmpp:" + destination, new Text(destination));
            return true;
        }

        throw new InvalidOperationException();
    }
}