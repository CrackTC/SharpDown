using System.Globalization;
using System.Reflection;
using System.Security;
using System.Text;
using System.Xml.Linq;

namespace CrackTC.SharpDown.Parsing;

internal static class TextUtils
{
    private const char Tab = '\u0009';
    public const char Space = '\u0020';
    private const char LineFeed = '\u000a';
    private const char FormFeed = '\u000c';
    private const char CarriageReturn = '\u000d';

    private static readonly Dictionary<string, string> EntityDictionary;

    static TextUtils()
    {
        EntityDictionary = new Dictionary<string, string>();
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("CrackTC.SharpDown.Parsing.entities.bin")!;
        using var reader = new BinaryReader(stream);
        var count = reader.ReadInt32();
        for (var i = 0; i < count; i++)
        {
            var key = reader.ReadString();
            var value = reader.ReadString();
            EntityDictionary[key] = value;
        }
    }

    public static bool IsTab(this char character)
    {
        return character is Tab;
    }

    public static bool IsSpace(this char character)
    {
        return character is Space;
    }

    private static bool IsInZs(this char character)
    {
        return char.GetUnicodeCategory(character) is UnicodeCategory.SpaceSeparator;
    }

    private static bool IsLineFeed(this char character)
    {
        return character is LineFeed;
    }

    private static bool IsFormFeed(this char character)
    {
        return character is FormFeed;
    }

    private static bool IsCarriageReturn(this char character)
    {
        return character is CarriageReturn;
    }

    public static bool IsBlankLine(this ReadOnlySpan<char> line)
    {
        line = ReadLine(line, out _, out _, out _);
        foreach (var ch in line)
            if (!ch.IsSpace() && !ch.IsTab())
                return false;
        return true;
    }

    public static bool IsLineEnding(this char character)
    {
        return character.IsLineFeed() || character.IsCarriageReturn();
    }

    public static bool TryRemoveLineEnding(this ref ReadOnlySpan<char> text)
    {
        if (text.IsEmpty) return true;
        if (text[0].IsLineFeed())
        {
            text = text[1..];
            return true;
        }

        if (!text[0].IsCarriageReturn()) return false;
        if (text.Length > 1 && text[1].IsLineFeed()) text = text[2..];
        else text = text[1..];
        return true;
    }

    public static bool IsUnicodeWhiteSpace(this char character)
    {
        return character.IsInZs() ||
               character.IsTab() ||
               character.IsLineFeed() ||
               character.IsFormFeed() ||
               character.IsCarriageReturn();
    }

    private static bool IsAsciiControl(this char character)
    {
        return character is '\u007f' or <= '\u001f';
    }

    public static bool IsAsciiPunctuation(this char character)
    {
        return character is >= '\u0021' and <= '\u002f'
            or >= '\u003a' and <= '\u0040'
            or >= '\u005b' and <= '\u0060'
            or >= '\u007b' and <= '\u007e';
    }

    public static bool IsUnicodePunctuation(this char character)
    {
        return character.IsAsciiPunctuation() ||
               char.GetUnicodeCategory(character)
                   is UnicodeCategory.ConnectorPunctuation
                   or UnicodeCategory.DashPunctuation
                   or UnicodeCategory.ClosePunctuation
                   or UnicodeCategory.FinalQuotePunctuation
                   or UnicodeCategory.InitialQuotePunctuation
                   or UnicodeCategory.OtherPunctuation
                   or UnicodeCategory.OpenPunctuation;
    }

    public static bool TryRemoveTagInnerSpaces(this ref ReadOnlySpan<char> text)
    {
        var index = text.CountLeadingCharacter(ch => ch.IsSpace() || ch.IsTab());

        var tmp = text[index..];
        if (tmp.TryRemoveLineEnding())
        {
            if (tmp.IsEmpty)
            {
                text = tmp;
                return true;
            }

            index = tmp.CountLeadingCharacter(ch => ch.IsSpace() || ch.IsTab());
            if (tmp[index].IsLineEnding()) return false;
            tmp = tmp[index..];
        }

        text = tmp;
        return true;
    }

    public static bool TryReadTagName(this ref ReadOnlySpan<char> text, out string tagName)
    {
        tagName = string.Empty;

        if (text.IsEmpty) return false;
        if (!char.IsAsciiLetter(text[0])) return false;

        int i;
        for (i = 1; i < text.Length; i++)
            if (!char.IsAsciiLetter(text[i]) && !char.IsAsciiDigit(text[i]) && text[i] != '-')
                break;

        tagName = text[..i].ToString();
        text = text[i..];
        return true;
    }

    private static bool TryReadAttributeName(this ref ReadOnlySpan<char> text)
    {
        if (text.IsEmpty) return false;
        if (!char.IsAsciiLetter(text[0]) && !"_:".Contains(text[0])) return false;

        int i;
        for (i = 1; i < text.Length; i++)
            if (!char.IsAsciiLetter(text[i]) && !char.IsAsciiDigit(text[i]) && !"_.:-".Contains(text[i]))
                break;

        text = text[i..];
        return true;
    }

    private static bool TryReadUnquotedAttributeValue(this ref ReadOnlySpan<char> text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (!ch.IsSpace() && !ch.IsTab() && !ch.IsLineEnding() && !"\"'=<>`".Contains(ch)) continue;
            if (i == 0) return false;
            text = text[i..];
            return true;
        }

        return false;
    }

    private static bool TryReadSingleQuotedAttributeValue(this ref ReadOnlySpan<char> text)
    {
        if (!text.StartsWith("'")) return false;

        for (var i = 1; i < text.Length; i++)
        {
            if (text[i] != '\'') continue;
            text = text[(i + 1)..];
            return true;
        }

        return false;
    }

    private static bool TryReadDoubleQuotedAttributeValue(this ref ReadOnlySpan<char> text)
    {
        if (!text.StartsWith("\"")) return false;

        for (var i = 1; i < text.Length; i++)
        {
            if (text[i] != '"') continue;
            text = text[(i + 1)..];
            return true;
        }

        return false;
    }

    private static bool TryReadAttributeValue(this ref ReadOnlySpan<char> text)
    {
        if (text.TryReadUnquotedAttributeValue()) return true;
        return text.TryReadSingleQuotedAttributeValue() || text.TryReadDoubleQuotedAttributeValue();
    }

    private static void TryReadAttributeValueSpecification(this ref ReadOnlySpan<char> text)
    {
        var tmp = text;
        if (!tmp.TryRemoveTagInnerSpaces()) return;
        if (!tmp.StartsWith("=")) return;
        tmp = tmp[1..];
        if (!tmp.TryRemoveTagInnerSpaces()) return;
        if (!tmp.TryReadAttributeValue()) return;
        text = tmp;
    }

    private static bool TryReadAttribute(this ref ReadOnlySpan<char> text)
    {
        var tmp = text;
        if (!tmp.TryRemoveTagInnerSpaces()) return false;
        if (tmp == text) return false;
        if (!tmp.TryReadAttributeName()) return false;
        tmp.TryReadAttributeValueSpecification();
        text = tmp;
        return true;
    }

    public static bool TryReadOpenTag(this ref ReadOnlySpan<char> text, out string tag)
    {
        tag = string.Empty;

        var tmp = text;

        if (!tmp.StartsWith("<")) return false;
        tmp = tmp[1..];
        if (!tmp.TryReadTagName(out _)) return false;
        while (tmp.TryReadAttribute())
        {
        }

        if (!tmp.TryRemoveTagInnerSpaces()) return false;
        if (tmp.StartsWith("/")) tmp = tmp[1..];
        if (!tmp.StartsWith(">")) return false;
        tmp = tmp[1..];

        tag = text[..^tmp.Length].ToString();
        text = tmp;
        return true;
    }

    public static bool TryReadClosingTag(this ref ReadOnlySpan<char> text, out string tag)
    {
        tag = string.Empty;

        var tmp = text;

        if (!tmp.StartsWith("</")) return false;
        tmp = tmp[2..];
        if (!tmp.TryReadTagName(out _)) return false;
        if (!tmp.TryRemoveTagInnerSpaces()) return false;
        if (!tmp.StartsWith(">")) return false;
        tmp = tmp[1..];

        tag = text[..^tmp.Length].ToString();
        text = tmp;
        return true;
    }

    public static bool TryReadHtmlComment(this ref ReadOnlySpan<char> text, out string tag)
    {
        tag = string.Empty;

        if (!text.StartsWith("<!--")) return false;
        var endIndex = text[4..].IndexOf("-->") + 4;
        if (endIndex is 3) return false;

        var commentSpan = text[4..endIndex];
        if (commentSpan.StartsWith(">")
            || commentSpan.StartsWith("->")
            || commentSpan.EndsWith("-")
            || commentSpan.IndexOf("--") is not -1) return false;

        tag = text[..(endIndex + 3)].ToString();
        text = text[(endIndex + 3)..];
        return true;
    }

    public static bool TryReadProcessingInstruction(this ref ReadOnlySpan<char> text, out string tag)
    {
        tag = string.Empty;

        if (!text.StartsWith("<?")) return false;
        var endIndex = text[2..].IndexOf("?>") + 2;
        if (endIndex is 1) return false;

        tag = text[..(endIndex + 2)].ToString();
        text = text[(endIndex + 2)..];
        return true;
    }

    public static bool TryReadDeclaration(this ref ReadOnlySpan<char> text, out string tag)
    {
        tag = string.Empty;

        if (!text.StartsWith("<!")) return false;
        if (text.Length < 3 || !char.IsAsciiLetter(text[2])) return false;

        var endIndex = text.IndexOf('>');
        if (endIndex is -1) return false;

        tag = text[..(endIndex + 1)].ToString();
        text = text[(endIndex + 1)..];
        return true;
    }

    public static bool TryReadCdataSection(this ref ReadOnlySpan<char> text, out string tag)
    {
        tag = string.Empty;

        if (!text.StartsWith("<![CDATA[")) return false;
        var endIndex = text.IndexOf("]]>");
        if (endIndex is -1) return false;

        tag = text[..(endIndex + 3)].ToString();
        text = text[(endIndex + 3)..];
        return true;
    }

    public static bool TryReadLinkLabel(this ref ReadOnlySpan<char> text, out string label)
    {
        label = string.Empty;

        var tmp = text;

        if (!tmp.StartsWith("[")) return false;
        tmp = tmp[1..];
        if (!tmp.TryReadUtilUnescaped(']', out label) || label.Length > 999) return false;

        var labelSpan = label.AsSpan();
        if (labelSpan.TryReadUtilUnescaped('[', out _)) return false;
        if (label.All(ch => ch.IsSpace() || ch.IsTab() || ch.IsLineEnding())) return false;

        text = tmp[1..];
        return true;
    }

    private static bool TryReadWrappedLinkDestination(this ref ReadOnlySpan<char> text, out string destination)
    {
        destination = string.Empty;

        var tmp = text;

        if (!tmp.StartsWith("<")) return false;
        tmp = tmp[1..];
        if (!tmp.TryReadUtilUnescaped('>', out destination)) return false;
        if (destination.Any(ch => ch.IsLineEnding())) return false;
        var destinationSpan = destination.AsSpan();
        if (destinationSpan.TryReadUtilUnescaped('<', out _)) return false;

        text = tmp[1..];
        return true;
    }

    private static bool TryReadUnwrappedLinkDestination(this ref ReadOnlySpan<char> text, out string destination)
    {
        destination = string.Empty;

        if (text.StartsWith("<")) return false;

        var index = 0;
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

        if (index == text.Length) destination = text.ToString();

        if (destination.Length is 0) return false;

        var matchCount = 0;
        index = 0;
        while (index < destination.Length)
        {
            var ch = destination[index];
            if (ch is '\\')
            {
                index += 2;
                continue;
            }

            switch (ch)
            {
                case '(':
                    matchCount++;
                    break;
                case ')' when matchCount > 0:
                    matchCount--;
                    break;
                case ')':
                    destination = destination[..index];
                    text = text[index..];
                    return true;
            }

            index++;
        }

        if (matchCount != 0) return false;

        text = text[index..];
        return true;
    }

    public static bool TryReadLinkDestination(this ref ReadOnlySpan<char> text, out string destination)
    {
        return text.TryReadWrappedLinkDestination(out destination)
               || text.TryReadUnwrappedLinkDestination(out destination);
    }

    private static bool TryReadDoubleQuotedLinkTitle(this ref ReadOnlySpan<char> text, out string title)
    {
        title = string.Empty;

        var tmp = text;

        if (!tmp.StartsWith("\"")) return false;
        tmp = tmp[1..];
        if (!tmp.TryReadUtilUnescaped('"', out title)) return false;

        var titleSpan = title.AsSpan();
        _ = ReadLine(titleSpan, out titleSpan, out _, out _);

        while (!titleSpan.IsEmpty)
            if (ReadLine(titleSpan, out titleSpan, out _, out _).IsBlankLine())
                return false;

        text = tmp[1..];
        return true;
    }

    private static bool TryReadSingleQuotedLinkTitle(this ref ReadOnlySpan<char> text, out string title)
    {
        title = string.Empty;

        var tmp = text;

        if (!tmp.StartsWith("'")) return false;
        tmp = tmp[1..];
        if (!tmp.TryReadUtilUnescaped('\'', out title)) return false;

        var titleSpan = title.AsSpan();
        _ = ReadLine(titleSpan, out titleSpan, out _, out _);

        while (!titleSpan.IsEmpty)
            if (ReadLine(titleSpan, out titleSpan, out _, out _).IsBlankLine())
                return false;

        text = tmp[1..];
        return true;
    }

    private static bool TryReadWrappedLinkTitle(this ref ReadOnlySpan<char> text, out string title)
    {
        title = string.Empty;

        var tmp = text;

        if (!tmp.StartsWith("(")) return false;
        tmp = tmp[1..];
        if (!tmp.TryReadUtilUnescaped(')', out title)) return false;

        var titleSpan = title.AsSpan();
        if (titleSpan.TryReadUtilUnescaped('(', out _)) return false;

        _ = ReadLine(titleSpan, out titleSpan, out _, out _);

        while (!titleSpan.IsEmpty)
            if (ReadLine(titleSpan, out titleSpan, out _, out _).IsBlankLine())
                return false;

        text = tmp[1..];
        return true;
    }

    public static bool TryReadLinkTitle(this ref ReadOnlySpan<char> text, out string title)
    {
        return text.TryReadDoubleQuotedLinkTitle(out title)
               || text.TryReadSingleQuotedLinkTitle(out title)
               || text.TryReadWrappedLinkTitle(out title);
    }

    public static bool TryReadAbsoluteUri(this ref ReadOnlySpan<char> text, out string uri)
    {
        uri = string.Empty;
        if (text.IsEmpty) return false;

        // read scheme
        if (!char.IsAsciiLetter(text[0])) return false;

        int i;
        for (i = 1; i < text.Length; i++)
        {
            var ch = text[i];
            if (!char.IsAsciiLetter(ch) && !char.IsAsciiDigit(ch) && !"+.-".Contains(ch))
                break;
        }

        if (i is < 2 or > 32) return false;

        // read colon
        if (text[i..].StartsWith(":") is false) return false;

        // read remaining
        int j;
        for (j = i + 1; j < text.Length; j++)
        {
            var ch = text[j];
            if (ch.IsAsciiControl() || ch.IsSpace() || ch is '<' or '>') break;
        }

        uri = text[..j].ToString();
        text = text[j..];
        return true;
    }

    public static string MarkAsParagraph(this ReadOnlySpan<char> text)
    {
        return text.ToString() + '\0';
    }

    /// <summary>
    ///     Returns current line of text, excluding CR/LF.
    /// </summary>
    /// <param name="text">The char span where the line should be read from.</param>
    /// <param name="remaining">The remaining text.</param>
    /// <param name="columnNumber">Column number of line start.</param>
    /// <param name="markedAsParagraph">Whether the line is explicitly marked as paragraph.</param>
    /// <returns></returns>
    public static ReadOnlySpan<char> ReadLine(ReadOnlySpan<char> text, out ReadOnlySpan<char> remaining,
        out int columnNumber, out bool markedAsParagraph)
    {
        var index = 0;
        for (var tmp = text; tmp.IsEmpty is false; tmp = tmp[1..], index++)
        {
            if (!tmp.TryRemoveLineEnding()) continue;
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

    public static int CountLeadingCharacter(this ReadOnlySpan<char> text, Predicate<char> predicate,
        int limit = int.MaxValue)
    {
        var index = 0;
        while (index < text.Length && predicate(text[index]))
        {
            index++;
            if (index == limit) return index;
        }

        return index;
    }

    public static int CountTrailingCharacter(this ReadOnlySpan<char> text, Predicate<char> predicate,
        int limit = int.MaxValue)
    {
        var index = 1;
        while (index <= text.Length && predicate(text[^index]))
        {
            index++;
            if (index == limit) return index;
        }

        return index - 1;
    }

    public static int GetTabSpaces(this int columnNumber)
    {
        return 4 - (columnNumber & 3);
    }

    public static (int Count, int Index, int TabRemainingSpaces) CountLeadingSpace(this ReadOnlySpan<char> text,
        int columnNumber, int limit)
    {
        var index = 0;
        var count = 0;
        foreach (var ch in text)
        {
            if (count >= limit) return (limit, index, count - limit);

            if (ch.IsSpace()) count++;
            else if (ch.IsTab()) count += (columnNumber + count).GetTabSpaces();
            else break;

            index++;
        }

        return count > limit
            ? (limit, index, count - limit)
            : (count, index, 0);
    }

    public static ReadOnlySpan<char> TrimTabAndSpace(this ReadOnlySpan<char> text)
    {
        var start = 0;
        var end = text.Length;
        while (start < text.Length && (text[start].IsSpace() || text[start].IsTab())) start++;
        while (end > 0 && (text[end - 1].IsSpace() || text[end - 1].IsTab())) end--;

        return start > end ? ReadOnlySpan<char>.Empty : text[start..end];
    }

    public static string NormalizeLabel(this string label)
    {
        label = label.Trim(Space, Tab, CarriageReturn, LineFeed);

        var builder = new StringBuilder(label.Length);

        var spaceOpen = false;
        foreach (var ch in label)
        {
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

    internal static bool TryReadUtilUnescaped(this ref ReadOnlySpan<char> text, char ending, out string content)
    {
        var builder = new StringBuilder();
        var tmp = text;
        while (!tmp.IsEmpty)
        {
            var line = ReadLine(tmp, out var remaining, out _, out _);
            for (var i = 0; i < line.Length; i++)
                if (line[i] == '\\')
                {
                    if (i >= line.Length - 1) continue;
                    if (line[i + 1].IsAsciiPunctuation()) i++;
                }
                else if (line[i] == ending)
                {
                    builder.Append(line[..i]);
                    content = builder.ToString();
                    tmp.ReadColumnNumber();
                    text = tmp[i..];
                    return true;
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
        for (var i = 0; i < text.Length; i++)
            if (text[i] != '\\' || i == text.Length - 1 || text[i + 1].IsAsciiPunctuation() is false)
            {
                builder.Append(text[i]);
            }
            else
            {
                i++;
                builder.Append(text[i]);
            }

        return builder.ToString();
    }

    public static string FlattenText(this XElement? element)
    {
        if (element is null) return string.Empty;

        var builder = new StringBuilder();
        foreach (var node in element.Nodes())
            switch (node)
            {
                case XText text:
                    builder.Append(text);
                    break;
                case XElement childElement:
                    builder.Append(childElement.FlattenText());
                    break;
            }

        return builder.ToString();
    }

    public static string HtmlEscape(this string text)
    {
        return SecurityElement.Escape(text);
    }

    public static string HtmlUnescape(this ReadOnlySpan<char> text)
    {
        var builder = new StringBuilder(text.Length);
        var semicolons = new LinkedList<int>();
        int i;
        for (i = 0; i < text.Length; i++)
            if (text[i] == ';')
                semicolons.AddLast(i);

        var node = semicolons.First;
        for (i = 0; i < text.Length && node != null; i++)
        {
            if (text[i] == '\\')
            {
                builder.Append(text[i]).Append(text[i + 1]);
                i++;
                continue;
            }

            if (text[i] == '&')
            {
                while (node != null && node.Value < i) node = node.Next;
                if (node == null) break;

                var numeric = false;
                var hex = false;
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
                            || !int.TryParse(text[(i + 3)..node.Value], NumberStyles.AllowHexSpecifier, null,
                                out codePoint))
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
                    continue;
                }

                if (EntityDictionary.TryGetValue(text[i..(node.Value + 1)].ToString(), out var characters))
                {
                    builder.Append(characters);
                    i = node.Value;
                    node = node.Next;
                    continue;
                }

                builder.Append('&');
                continue;
            }

            builder.Append(text[i]);
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
        var k = 1;
        var result = 0;

        while (heading.IsEmpty is false && heading[0] == '\0')
        {
            result += heading[1] * k;
            k *= sizeof(char);
            heading = heading[2..];
        }

        return result;
    }
}