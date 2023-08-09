using System.Text;
using System.Text.RegularExpressions;
using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Leaf;

namespace CrackTC.SharpDown.Parsing.Block.Leaf;

internal partial class HtmlBlockParser : IMarkdownBlockParser
{
    [GeneratedRegex("^(?:pre|script|style|textarea)$", RegexOptions.IgnoreCase)]
    private static partial Regex TextRegex();

    [GeneratedRegex("</(?:pre|script|style|textarea)>", RegexOptions.IgnoreCase)]
    private static partial Regex TextEndingRegex();

    [GeneratedRegex("^(?:address|article|aside|base|basefont|blockquote|body|caption|center|col|colgroup|dd|details|dialog|dir|div|dl|dt|fieldset|figcaption|figure|footer|form|frame|frameset|h1|h2|h3|h4|h5|h6|head|header|hr|html|iframe|legend|li|link|main|menu|menuitem|nav|noframes|ol|optgroup|option|p|param|section|source|summary|table|tbody|td|tfoot|th|thead|title|tr|track|ul)$", RegexOptions.IgnoreCase)]
    private static partial Regex MiscRegex();

    private static HtmlBlockType GetBlockType(ReadOnlySpan<char> text)
    {
        var line = TextUtils.ReadLine(text, out _, out var columnNumber, out _);

        if (line.IsEmpty) return HtmlBlockType.None;
        var (count, index, _) = line.CountLeadingSpace(columnNumber, 4);
        if (count == 4) return HtmlBlockType.None;

        line = line[index..];

        if (line.StartsWith("<"))
        {
            var tmp = line[1..];
            if (tmp.TryReadTagName(out var tagName) && TextRegex().IsMatch(tagName)) return HtmlBlockType.Text;
        }
        if (line.StartsWith("<!--")) return HtmlBlockType.Comment;
        if (line.StartsWith("<?")) return HtmlBlockType.ProcessingInstruction;
        if (line.StartsWith("<!") && line.Length > 2 && char.IsAsciiLetter(line[2])) return HtmlBlockType.Declaration;
        if (line.StartsWith("<![CDATA[")) return HtmlBlockType.Cdata;
        if (line.StartsWith("<"))
        {
            var tmp = line[1..];
            if (tmp.StartsWith("/")) tmp = tmp[1..];

            if (tmp.TryReadTagName(out var tagName) && MiscRegex().IsMatch(tagName))
            {
                if (tmp.IsEmpty || tmp[0].IsSpace() || tmp[0].IsTab()) return HtmlBlockType.Misc;
                if (tmp[0] == '/') tmp = tmp[1..];
                if (tmp.StartsWith(">")) return HtmlBlockType.Misc;
            }
        }

        if ((line.TryReadOpenTag(out _) || line.TryReadClosingTag(out _)) && line.IsBlankLine())
            return HtmlBlockType.Any;

        return HtmlBlockType.None;
    }

    private static ReadOnlySpan<char> Skip(ReadOnlySpan<char> text, out string content, out HtmlBlockType type)
    {
        content = string.Empty;

        type = GetBlockType(text);

        if (type == HtmlBlockType.None)
        {
            return text;
        }

        var remaining = text;
        var builder = new StringBuilder();
        var lineEndingFlag = false;
        while (true)
        {
            var line = TextUtils.ReadLine(remaining, out remaining, out _, out _);
            builder.Append(lineEndingFlag ? "\n" : "").Append(line);
            lineEndingFlag = true;
            var assert = type switch
            {
                HtmlBlockType.Text => TextEndingRegex().IsMatch(line),
                HtmlBlockType.Comment => line.IndexOf("-->") != -1,
                HtmlBlockType.ProcessingInstruction => line.IndexOf("?>") != -1,
                HtmlBlockType.Declaration => line.Contains('>'),
                HtmlBlockType.Cdata => line.IndexOf("]]>") != -1,
                HtmlBlockType.Misc or HtmlBlockType.Any => remaining.IsBlankLine(),
                _ => throw new InvalidOperationException("Unexpected HtmlBlockType")
            };

            if (assert || remaining.IsEmpty)
            {
                content = builder.ToString();
                return remaining;
            }
        }
    }

    public bool TryReadAndParse(ref ReadOnlySpan<char> text, MarkdownBlock father, IEnumerable<IMarkdownBlockParser> blockParsers)
    {
        var remaining = Skip(text, out var content, out var type);
        if (remaining == text)
        {
            return false;
        }

        if (type == HtmlBlockType.Any && father.LastChild is Paragraph)
        {
            return false;
        }

        text = remaining;
        father.Children.Add(new HtmlBlock(content));
        return true;
    }
}
