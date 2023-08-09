using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace CrackTC.SharpDown.Parsing;

internal static class TextUtils
{
    public const char Tab = '\u0009';
    public const char Space = '\u0020';
    public const char LineFeed = '\u000a';
    public const char FormFeed = '\u000c';
    public const char CarriageReturn = '\u000d';

    public static bool IsTab(this char character) => character is Tab;
    public static bool IsSpace(this char character) => character is Space;
    public static bool IsInZs(this char character) => char.GetUnicodeCategory(character) is UnicodeCategory.SpaceSeparator;
    public static bool IsLineFeed(this char character) => character is LineFeed;
    public static bool IsFormFeed(this char character) => character is FormFeed;
    public static bool IsCarriageReturn(this char character) => character is CarriageReturn;
    public static bool IsBlankLine(this ReadOnlySpan<char> line)
    {
        line = ReadLine(line, out _, out _, out _);
        foreach (var ch in line) if (!ch.IsSpace() && !ch.IsTab()) return false;
        return true;
    }
    public static bool IsLineEnding(this char character) => character.IsLineFeed() || character.IsCarriageReturn();
    public static bool TryRemoveLineEnding(this ref ReadOnlySpan<char> text)
    {
        if (text.IsEmpty) return true;
        if (text[0].IsLineFeed())
        {
            text = text[1..];
            return true;
        }
        else if (text[0].IsCarriageReturn())
        {
            if (text.Length > 1 && text[1].IsLineFeed())
            {
                text = text[2..];
            }
            else
            {
                text = text[1..];
            }

            return true;
        }
        return false;
    }
    public static bool IsUnicodeWhiteSpace(this char character) =>
        character.IsInZs() ||
        character.IsTab() ||
        character.IsLineFeed() ||
        character.IsFormFeed() ||
        character.IsCarriageReturn();
    public static bool IsAsciiControl(this char character) =>
        character is '\u007f'
            or >= '\u0000' and <= '\u001f';
    public static bool IsAsciiPunctuation(this char character) =>
        character is >= '\u0021' and <= '\u002f'
                  or >= '\u003a' and <= '\u0040'
                  or >= '\u005b' and <= '\u0060'
                  or >= '\u007b' and <= '\u007e';
    public static bool IsUnicodePunctuation(this char character) =>
        character.IsAsciiPunctuation() ||
        char.GetUnicodeCategory(character)
            is UnicodeCategory.ConnectorPunctuation
            or UnicodeCategory.DashPunctuation
            or UnicodeCategory.ClosePunctuation
            or UnicodeCategory.FinalQuotePunctuation
            or UnicodeCategory.InitialQuotePunctuation
            or UnicodeCategory.OtherPunctuation
            or UnicodeCategory.OpenPunctuation;

    public static bool TryRemoveTagInnerSpaces(this ref ReadOnlySpan<char> text)
    {
        var index = text.CountLeadingChracter(ch => ch.IsSpace() || ch.IsTab());

        var tmp = text[index..];
        if (tmp.TryRemoveLineEnding())
        {
            if (tmp.IsEmpty)
            {
                text = tmp;
                return true;
            }

            index = tmp.CountLeadingChracter(ch => ch.IsSpace() || ch.IsTab());
            if (tmp[index].IsLineEnding())
            {
                return false;
            }
            tmp = tmp[index..];
        }

        text = tmp;
        return true;
    }

    public static bool TryReadTagName(this ref ReadOnlySpan<char> text, out string tagName)
    {
        tagName = string.Empty;

        if (text.IsEmpty)
        {
            return false;
        }

        if (char.IsAsciiLetter(text[0]))
        {
            int i;
            for (i = 1; i < text.Length; i++)
            {
                if ((char.IsAsciiLetter(text[i]) || char.IsAsciiDigit(text[i]) || text[i] is '-') is false)
                {
                    break;
                }
            }

            tagName = text[..i].ToString();
            text = text[i..];
            return true;
        }
        else
        {
            return false;
        }
    }

    public static bool TryReadAttributeName(this ref ReadOnlySpan<char> text, out string attributeName)
    {
        attributeName = string.Empty;

        if (text.IsEmpty)
        {
            return false;
        }

        if (char.IsAsciiLetter(text[0]) || text[0] is '_' or ':')
        {
            int i;
            for (i = 1; i < text.Length; i++)
            {
                if (!(char.IsAsciiLetter(text[i]) || char.IsAsciiDigit(text[i]) || text[i] is '_' or '.' or ':' or '-'))
                {
                    break;
                }
            }

            attributeName = text[..i].ToString();
            text = text[i..];
            return true;
        }
        else
        {
            return false;
        }
    }

    public static bool TryReadUnquotedAttributeValue(this ref ReadOnlySpan<char> text, out string unquotedAttributeValue)
    {
        unquotedAttributeValue = string.Empty;
        for (int i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch.IsSpace() || ch.IsTab() || ch.IsLineEnding() || ch is '"' or '\'' or '=' or '<' or '>' or '`')
            {
                if (i == 0)
                {
                    return false;
                }
                unquotedAttributeValue = text[..i].ToString();
                text = text[i..];
                return true;
            }
        }

        return false;
    }

    public static bool TryReadSingleQuotedAttributeValue(this ref ReadOnlySpan<char> text, out string singleQuotedAttributeValue)
    {
        singleQuotedAttributeValue = string.Empty;
        if (text.StartsWith("'") is false)
        {
            return false;
        }

        for (int i = 1; i < text.Length; i++)
        {
            if (text[i] is '\'')
            {
                singleQuotedAttributeValue = text[..(i + 1)].ToString();
                text = text[(i + 1)..];
                return true;
            }
        }

        return false;
    }

    public static bool TryReadDoubleQuotedAttributeValue(this ref ReadOnlySpan<char> text, out string doubleQuotedAttributeValue)
    {
        doubleQuotedAttributeValue = string.Empty;
        if (text.StartsWith("\"") is false)
        {
            return false;
        }

        for (int i = 1; i < text.Length; i++)
        {
            if (text[i] is '"')
            {
                doubleQuotedAttributeValue = text[..(i + 1)].ToString();
                text = text[(i + 1)..];
                return true;
            }
        }

        return false;
    }

    public static bool TryReadAttributeValue(this ref ReadOnlySpan<char> text, out string attributeValue)
    {
        if (text.TryReadUnquotedAttributeValue(out attributeValue))
        {
            return true;
        }
        else if (text.TryReadSingleQuotedAttributeValue(out attributeValue))
        {
            return true;
        }
        else if (text.TryReadDoubleQuotedAttributeValue(out attributeValue))
        {
            return true;
        }

        return false;
    }

    public static bool TryReadAttributeValueSpecification(this ref ReadOnlySpan<char> text, out string attributeValueSpecification)
    {
        attributeValueSpecification = string.Empty;

        var tmp = text;
        if (tmp.TryRemoveTagInnerSpaces() is false)
        {
            return false;
        }
        if (tmp.StartsWith("=") is false)
        {
            return false;
        }

        tmp = tmp[1..];
        if (tmp.TryRemoveTagInnerSpaces() is false)
        {
            return false;
        }

        if (tmp.TryReadAttributeValue(out _))
        {
            attributeValueSpecification = text[..(text.Length - tmp.Length)].ToString();
            text = tmp;
            return true;
        }

        return false;
    }

    public static bool TryReadAttribute(this ref ReadOnlySpan<char> text, out string attribute)
    {
        attribute = string.Empty;

        var tmp = text;
        if (tmp.TryRemoveTagInnerSpaces() is false) return false;
        if (tmp == text) return false;
        if (tmp.TryReadAttributeName(out _) is false) return false;

        tmp.TryReadAttributeValueSpecification(out _);

        attribute = text[..(text.Length - tmp.Length)].ToString();
        text = tmp;
        return true;
    }

    public static bool TryReadOpenTag(this ref ReadOnlySpan<char> text, out string tag)
    {
        tag = string.Empty;

        var tmp = text;
        if (tmp.StartsWith("<") is false)
        {
            return false;
        }

        tmp = tmp[1..];

        if (tmp.TryReadTagName(out _) is false)
        {
            return false;
        }

        while (tmp.TryReadAttribute(out _)) ;

        if (tmp.TryRemoveTagInnerSpaces() is false)
        {
            return false;
        }

        if (tmp.StartsWith("/"))
        {
            tmp = tmp[1..];
        }

        if (tmp.StartsWith(">") is false)
        {
            return false;
        }

        tmp = tmp[1..];

        tag = text[..(text.Length - tmp.Length)].ToString();
        text = tmp;
        return true;
    }

    public static bool TryReadClosingTag(this ref ReadOnlySpan<char> text, out string tag)
    {
        tag = string.Empty;

        var tmp = text;
        if (tmp.StartsWith("</") is false)
        {
            return false;
        }

        tmp = tmp[2..];

        if (tmp.TryReadTagName(out _) is false)
        {
            return false;
        }

        if (tmp.TryRemoveTagInnerSpaces() is false)
        {
            return false;
        }

        if (tmp.StartsWith(">") is false)
        {
            return false;
        }

        tmp = tmp[1..];

        tag = text[..(text.Length - tmp.Length)].ToString();
        text = tmp;
        return true;
    }

    public static bool TryReadHtmlComment(this ref ReadOnlySpan<char> text, out string tag)
    {
        if (text.StartsWith("<!--") is false)
        {
            tag = string.Empty;
            return false;
        }

        int endIndex = text[4..].IndexOf("-->") + 4;
        if (endIndex is 3)
        {
            tag = string.Empty;
            return false;
        }

        var commentSpan = text[4..endIndex];
        if (commentSpan.StartsWith(">")
            || commentSpan.StartsWith("->")
            || commentSpan.EndsWith("-")
            || commentSpan.IndexOf("--") is not -1)
        {
            tag = string.Empty;
            return false;
        }

        tag = text[..(endIndex + 3)].ToString();
        text = text[(endIndex + 3)..];
        return true;
    }

    public static bool TryReadProcessingInstruction(this ref ReadOnlySpan<char> text, out string tag)
    {
        if (text.StartsWith("<?") is false)
        {
            tag = string.Empty;
            return false;
        }

        int endIndex = text[2..].IndexOf("?>") + 2;
        if (endIndex is 1)
        {
            tag = string.Empty;
            return false;
        }

        tag = text[..(endIndex + 2)].ToString();
        text = text[(endIndex + 2)..];
        return true;
    }

    public static bool TryReadDeclaration(this ref ReadOnlySpan<char> text, out string tag)
    {
        if (text.StartsWith("<!") is false)
        {
            tag = string.Empty;
            return false;
        }

        if (text.Length < 3 || char.IsAsciiLetter(text[2]) is false)
        {
            tag = string.Empty;
            return false;
        }

        int endIndex = text.IndexOf('>');
        if (endIndex is -1)
        {
            tag = string.Empty;
            return false;
        }

        tag = text[..(endIndex + 1)].ToString();
        text = text[(endIndex + 1)..];
        return true;
    }

    public static bool TryReadCDATASection(this ref ReadOnlySpan<char> text, out string tag)
    {
        if (text.StartsWith("<![CDATA[") is false)
        {
            tag = string.Empty;
            return false;
        }

        int endIndex = text.IndexOf("]]>");
        if (endIndex is -1)
        {
            tag = string.Empty;
            return false;
        }

        tag = text[..(endIndex + 3)].ToString();
        text = text[(endIndex + 3)..];
        return true;
    }

    public static bool TryReadLinkLabel(this ref ReadOnlySpan<char> text, out string label)
    {
        var tmp = text;
        if (tmp.StartsWith("[") is false)
        {
            label = string.Empty;
            return false;
        }

        tmp = tmp[1..];
        if (tmp.TryReadUtilUnescaped(']', out label) is false || label.Length > 999)
        {
            return false;
        }

        var labelSpan = label.AsSpan();

        if (labelSpan.TryReadUtilUnescaped('[', out _))
        {
            return false;
        }

        if (label.All(ch => ch.IsSpace() || ch.IsTab() || ch.IsLineEnding()))
        {
            return false;
        }

        text = tmp[1..];
        return true;
    }

    public static bool TryReadWrappedLinkDestination(this ref ReadOnlySpan<char> text, out string destination)
    {
        destination = string.Empty;

        var tmp = text;
        if (tmp.StartsWith("<") is false)
        {
            return false;
        }
        tmp = tmp[1..];

        if (tmp.TryReadUtilUnescaped('>', out destination) is false)
        {
            return false;
        }

        if (destination.Any(ch => ch.IsLineEnding()))
        {
            return false;
        }

        var destinationSpan = destination.AsSpan();
        if (destinationSpan.TryReadUtilUnescaped('<', out _))
        {
            return false;
        }

        text = tmp[1..];
        return true;
    }

    public static bool TryReadUnwrappedLinkDestination(this ref ReadOnlySpan<char> text, out string destination)
    {
        destination = string.Empty;

        if (text.StartsWith("<"))
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            var ch = text[index];
            if (ch.IsAsciiControl() || ch.IsSpace())
            {
                destination = text[..index].ToString();
                break;
            }

            index++;
        }

        if (index == text.Length)
        {
            destination = text.ToString();
        }

        if (destination.Length is 0)
        {
            return false;
        }

        int matchCount = 0;
        index = 0;
        while (index < destination.Length)
        {
            var ch = destination[index];

            if (ch is '\\')
            {
                index += 2;
            }
            else
            {
                if (ch is '(')
                {
                    matchCount++;
                }
                else if (ch is ')')
                {
                    if (matchCount > 0)
                    {
                        matchCount--;
                    }
                    else
                    {
                        destination = destination[..index];
                        text = text[index..];
                        return true;
                    }
                }

                index++;
            }
        }

        if (matchCount is not 0)
        {
            return false;
        }

        text = text[index..];
        return true;
    }

    public static bool TryReadLinkDestination(this ref ReadOnlySpan<char> text, out string destination)
        => text.TryReadWrappedLinkDestination(out destination)
        || text.TryReadUnwrappedLinkDestination(out destination);

    public static bool TryReadDoubleQuotedLinkTitle(this ref ReadOnlySpan<char> text, out string title)
    {
        title = string.Empty;

        var tmp = text;
        if (!tmp.StartsWith("\"")) return false;

        tmp = tmp[1..];
        if (!tmp.TryReadUtilUnescaped('"', out title)) return false;

        var titleSpan = title.AsSpan();
        _ = ReadLine(titleSpan, out titleSpan, out _, out _);

        while (!titleSpan.IsEmpty) if (ReadLine(titleSpan, out titleSpan, out _, out _).IsBlankLine()) return false;

        text = tmp[1..];
        return true;
    }

    public static bool TryReadSingleQuotedLinkTitle(this ref ReadOnlySpan<char> text, out string title)
    {
        title = string.Empty;

        var tmp = text;
        if (!tmp.StartsWith("'")) return false;

        tmp = tmp[1..];
        if (!tmp.TryReadUtilUnescaped('\'', out title)) return false;

        var titleSpan = title.AsSpan();
        _ = ReadLine(titleSpan, out titleSpan, out _, out _);

        while (!titleSpan.IsEmpty) if (ReadLine(titleSpan, out titleSpan, out _, out _).IsBlankLine()) return false;

        text = tmp[1..];
        return true;
    }

    public static bool TryReadWrappedLinkTitle(this ref ReadOnlySpan<char> text, out string title)
    {
        title = string.Empty;

        var tmp = text;
        if (!tmp.StartsWith("(")) return false;

        tmp = tmp[1..];

        if (!tmp.TryReadUtilUnescaped(')', out title)) return false;

        var titleSpan = title.AsSpan();
        if (titleSpan.TryReadUtilUnescaped('(', out _)) return false;

        _ = ReadLine(titleSpan, out titleSpan, out _, out _);

        while (!titleSpan.IsEmpty) if (ReadLine(titleSpan, out titleSpan, out _, out _).IsBlankLine()) return false;

        text = tmp[1..];
        return true;
    }

    public static bool TryReadLinkTitle(this ref ReadOnlySpan<char> text, out string title)
        => text.TryReadDoubleQuotedLinkTitle(out title)
        || text.TryReadSingleQuotedLinkTitle(out title)
        || text.TryReadWrappedLinkTitle(out title);

    public static bool TryReadAbsoluteUri(this ref ReadOnlySpan<char> text, out string uri)
    {
        if (text.IsEmpty)
        {
            uri = string.Empty;
            return false;
        }

        // read scheme
        if (char.IsAsciiLetter(text[0]) is false)
        {
            uri = string.Empty;
            return false;
        }

        int i;
        for (i = 1; i < text.Length; i++)
        {
            var ch = text[i];
            if (char.IsAsciiLetter(ch) is false
                && char.IsAsciiDigit(ch) is false
                && ch is not '+' and not '.' and not '-')
            {
                break;
            }
        }

        if (i < 2 || i > 32)
        {
            uri = string.Empty;
            return false;
        }

        // read colon
        if (text[i..].StartsWith(":") is false)
        {
            uri = string.Empty;
            return false;
        }

        // read remaining
        int j;
        for (j = i + 1; j < text.Length; j++)
        {
            var ch = text[j];
            if (ch.IsAsciiControl() || ch.IsSpace() || ch is '<' or '>')
            {
                break;
            }
        }

        uri = text[..j].ToString();
        text = text[j..];
        return true;
    }

    public static string MarkAsParagraph(this ReadOnlySpan<char> text) => text.ToString() + '\0';

    /// <summary>
    /// Returns current line of text, excluding CR/LF.
    /// </summary>
    /// <param name="text">The char span where the line should be read from.</param>
    /// <param name="remaining">The remaining text.</param>
    /// <returns></returns>
    public static ReadOnlySpan<char> ReadLine(ReadOnlySpan<char> text, out ReadOnlySpan<char> remaining, out int columnNumber, out bool markedAsParagraph)
    {
        int index = 0;
        for (var tmp = text; tmp.IsEmpty is false; tmp = tmp[1..], index++)
        {
            if (tmp.TryRemoveLineEnding())
            {
                remaining = tmp;
                var line = text[..index];
                columnNumber = line.ReadColumnNumber();
                if (!line.IsEmpty && line[^1] == '\0')
                {
                    markedAsParagraph = true;
                    return line[..^1];
                }
                markedAsParagraph = false;
                return line;
            }
        }

        // end with text ending
        remaining = ReadOnlySpan<char>.Empty;
        columnNumber = text.ReadColumnNumber();
        if (!text.IsEmpty && text[^1] == '\0')
        {
            markedAsParagraph = true;
            return text[..^1];
        }
        markedAsParagraph = false;
        return text;
    }

    public static int CountLeadingChracter(this ReadOnlySpan<char> text, Predicate<char> predicate, int limit = int.MaxValue)
    {
        int index = 0;
        while (index < text.Length && predicate(text[index]))
        {
            index++;
            if (index == limit)
            {
                return index;
            }
        }
        return index;
    }

    public static int CountTrailingChracter(this ReadOnlySpan<char> text, Predicate<char> predicate, int limit = int.MaxValue)
    {
        int index = 1;
        while (index <= text.Length && predicate(text[^index]))
        {
            index++;
            if (index == limit)
            {
                return index;
            }
        }
        return index - 1;
    }

    public static int GetTabSpaces(this int columnNumber)
    {
        return 4 - (columnNumber & 3);
    }
    public static (int Count, int Index, int TabRemainingSpaces) CountLeadingSpace(this ReadOnlySpan<char> text, int columnNumber, int limit)
    {
        int index = 0;
        int count = 0;
        foreach (var ch in text)
        {
            if (count >= limit) return (limit, index, count - limit);

            if (ch.IsSpace()) count++;
            else if (ch.IsTab()) count += (columnNumber + count).GetTabSpaces();
            else break;

            index++;
        }

        if (count > limit) return (limit, index, count - limit);
        return (count, index, 0);
    }

    public static ReadOnlySpan<char> TrimTabAndSpace(this ReadOnlySpan<char> text)
    {
        int start = 0;
        int end = text.Length;
        while (start < text.Length && (text[start].IsSpace() || text[start].IsTab()))
        {
            start++;
        }
        while (end > 0 && (text[end - 1].IsSpace() || text[end - 1].IsTab()))
        {
            end--;
        }

        if (start > end)
        {
            return ReadOnlySpan<char>.Empty;
        }
        return text[start..end];
    }

    public static string NormalizeLabel(this string label)
    {
        label = label.Trim(Space, Tab, CarriageReturn, LineFeed);

        var builder = new StringBuilder(label.Length);

        bool spaceOpen = false;
        for (int i = 0; i < label.Length; i++)
        {
            var ch = label[i];
            if (ch.IsSpace() || ch.IsTab() || ch.IsLineEnding())
            {
                spaceOpen = true;
                continue;
            }

            if (spaceOpen)
            {
                builder.Append(Space);
                spaceOpen = false;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }

    public static bool TryReadUtilUnescaped(this ref ReadOnlySpan<char> text, char ending, out string content)
    {
        var builder = new StringBuilder();
        var tmp = text;
        while (!tmp.IsEmpty)
        {
            var line = ReadLine(tmp, out var remaining, out _, out _);
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '\\')
                {
                    if (i < line.Length - 1)
                    {
                        if (line[i + 1].IsAsciiPunctuation())
                        {
                            i++;
                            continue;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
                else if (line[i] == ending)
                {
                    builder.Append(line[..i]);
                    content = builder.ToString();
                    tmp.ReadColumnNumber();
                    text = tmp[i..];
                    return true;
                }
            }
            tmp = remaining;
            builder.Append(line);
            builder.Append('\n');
        }

        content = string.Empty;
        return false;
    }

    public static string Unescape(this string text)
    {
        var builder = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '\\' || i == text.Length - 1 || text[i + 1].IsAsciiPunctuation() is false)
            {
                builder.Append(text[i]);
            }
            else
            {
                i++;
                builder.Append(text[i]);
            }
        }

        return builder.ToString();
    }

    public static string FlattenText(this XElement? element)
    {
        if (element is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var node in element.Nodes())
        {
            switch (node)
            {
                case XText text:
                    builder.Append(text.ToString());
                    break;
                case XElement childElement:
                    builder.Append(childElement.FlattenText());
                    break;
                default:
                    break;
            }
        }

        return builder.ToString();
    }

    public static string HtmlEscape(this string text)
    {
        return System.Security.SecurityElement.Escape(text);
    }

    private static readonly Dictionary<string, string> entityDictionary;

    public static string HtmlUnescape(this ReadOnlySpan<char> text)
    {
        var builder = new StringBuilder(text.Length);
        var semicolons = new LinkedList<int>();
        int i;
        for (i = 0; i < text.Length; i++) if (text[i] == ';') semicolons.AddLast(i);

        var node = semicolons.First;
        for (i = 0; i < text.Length && node != null; i++)
        {
            if (text[i] == '\\')
            {
                builder.Append(text[i]).Append(text[i + 1]);
                i++;
                continue;
            }
            else if (text[i] == '&')
            {
                while (node != null && node.Value < i) node = node.Next;
                if (node == null) break;

                bool numeric = false;
                bool hex = false;
                if (text[i + 1] == '#') // numeric
                {
                    numeric = true;
                    if (text[i + 2] is 'X' or 'x') hex = true;
                }

                if (numeric)
                {
                    int codePoint;
                    if (hex)
                    {
                        if (node.Value - (i + 3) is < 1 or > 6
                            || !int.TryParse(text[(i + 3)..node.Value], NumberStyles.AllowHexSpecifier, null, out codePoint))
                        {
                            builder.Append('&');
                            continue;
                        }
                    }
                    else if (node.Value - (i + 2) is < 1 or > 7
                            || !int.TryParse(text[(i + 2)..node.Value], NumberStyles.None, null, out codePoint))
                    {
                        builder.Append('&');
                        continue;
                    }

                    builder.Append(char.ConvertFromUtf32(codePoint));
                    i = node.Value;
                    node = node.Next;
                }
                else if (entityDictionary.TryGetValue(text[i..(node.Value + 1)].ToString(), out string? characters))
                {
                    builder.Append(characters);
                    i = node.Value;
                    node = node.Next;
                }
                else
                {
                    builder.Append('&');
                    continue;
                }
            }
            else
            {
                builder.Append(text[i]);
            }
        }

        builder.Append(text[i..]);
        return builder.ToString();
    }

    public static string GenerateHeading(this int columnNumber)
    {
        var builder = new StringBuilder();
        while (columnNumber != 0)
        {
            builder.Append('\0');
            builder.Append((char)(columnNumber % 65536));
            columnNumber /= 65536;
        }

        return builder.ToString();
    }

    public static int ReadColumnNumber(this ref ReadOnlySpan<char> heading)
    {
        int k = 1;
        int result = 0;

        while (heading.IsEmpty is false && heading[0] == '\0')
        {
            result += heading[1] * k;
            k *= sizeof(char);
            heading = heading[2..];
        }

        return result;
    }

    static TextUtils()
    {
        entityDictionary = new();
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CrackTC.SharpDown.Parsing.entities.json")!;
        var node = JsonSerializer.Deserialize<JsonNode>(stream)!;
        foreach (var item in node.AsObject())
        {
            entityDictionary[item.Key] = (string)item.Value!["characters"]!;
        }
    }
}
