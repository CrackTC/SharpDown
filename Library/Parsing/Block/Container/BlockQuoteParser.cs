using System.Text;
using CrackTC.SharpDown.Parsing.Block.Leaf;
using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Container;

namespace CrackTC.SharpDown.Parsing.Block.Container;

internal class BlockQuoteParser : IMarkdownBlockParser
{
    public bool TryReadAndParse(ref ReadOnlySpan<char> text,
        MarkdownBlock father,
        IEnumerable<IMarkdownBlockParser> parsers)
    {
        var remaining = Skip(text, father, parsers);
        if (remaining == text) return false;

        text = remaining;
        return true;
    }

    /// <summary>
    ///     Skip block quote marker.
    /// </summary>
    /// <param name="line">The line to skip block quote marker.</param>
    /// <param name="columnNumber">Column number of the start of <paramref name="line" />.</param>
    /// <param name="length">Total length of leading spaces and block quote marker.</param>
    /// <returns>
    ///     remaining of the line,
    ///     and if no block quote marker was skipped, return <paramref name="line" />.
    /// </returns>
    private static ReadOnlySpan<char> SkipBlockQuoteMarker(ReadOnlySpan<char> line, int columnNumber, out int length)
    {
        length = 0;
        var (count, index, _) = line.CountLeadingSpace(columnNumber, 4);

        if (count is 4) return line; // 4 spaces is too many
        if (!line[index..].StartsWith(">")) return line; // block quote marker is required

        length = index + 1;
        return line[length..];
    }

    /// <summary>
    ///     Append content of a block quote line to <paramref name="builder" />,
    ///     block quote mark should already be skipped.
    /// </summary>
    /// <param name="builder">The <see cref="StringBuilder" /> which content should be append to.</param>
    /// <param name="line">A block quote line with block quote mark skipped.</param>
    /// <param name="columnNumber">columnNumber of the start of the <paramref name="line" />.</param>
    private static void AppendBlockQuoteContent(StringBuilder builder, ReadOnlySpan<char> line, int columnNumber)
    {
        if (line.IsEmpty) return;

        if (line[0].IsSpace()) // one space consumed
            builder.Append((columnNumber + 1).GenerateHeading()).Append(line[1..]);
        else if (line[0].IsTab()) // one space consumed
            builder.Append((columnNumber + 1).GenerateHeading())
                .Append(TextUtils.Space, columnNumber.GetTabSpaces() - 1)
                .Append(line[1..]);
        else
            builder.Append(columnNumber.GenerateHeading()).Append(line);
    }

    private static bool HandleMarkedBlockQuote(ref ReadOnlySpan<char> text, StringBuilder builder,
        out ReadOnlySpan<char> line)
    {
        line = TextUtils.ReadLine(text, out text, out var columnNumber, out _);
        line = SkipBlockQuoteMarker(line, columnNumber, out var length);
        if (length == 0) return false;
        columnNumber += length;
        AppendBlockQuoteContent(builder, line, columnNumber);
        builder.Append('\n');
        return true;
    }

    private static ReadOnlySpan<char> Skip(ReadOnlySpan<char> text,
        MarkdownBlock father,
        IEnumerable<IMarkdownBlockParser> parsers)
    {
        var contentBuilder = new StringBuilder();
        var remaining = text;
        if (!HandleMarkedBlockQuote(ref remaining, contentBuilder, out _)) return text;

        BlockQuote result;
        while (!remaining.IsEmpty)
        {
            text = remaining;
            if (HandleMarkedBlockQuote(ref remaining, contentBuilder, out var line)) continue;

            // maybe continuation text
            result = new BlockQuote();
            MarkdownParser.ParseBlocks(contentBuilder.ToString(), result, parsers);

            if (!Utils.IsNestedLastParagraph(result))
            {
                father.Children.Add(result);
                return text;
            }

            if (MarkdownParser.ParseBlock(ref text, result,
                    parsers.Where(p => p is not IndentedCodeBlockParser))) // not continuation
            {
                father.Children.Add(result);
                father.Children.Add(result.LastChild!);
                result.Children.RemoveAt(result.Children.Count - 1);
                return text;
            }

            contentBuilder.Append(line.MarkAsParagraph());
            contentBuilder.Append('\n');
        }

        result = new BlockQuote();
        MarkdownParser.ParseBlocks(contentBuilder.ToString(), result, parsers);
        father.Children.Add(result);
        return ReadOnlySpan<char>.Empty;
    }
}