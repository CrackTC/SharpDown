using CrackTC.SharpDown.Parsing;
using CrackTC.SharpDown.Parsing.Block;
using CrackTC.SharpDown.Parsing.Block.Leaf;
using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Container;
using CrackTC.SharpDown.Structure.Block.Leaf;
using System.Text;

namespace CrackTC.SharpDown.Parsing.Block.Container;

internal class BlockQuoteParser : IMarkdownBlockParser
{
    /// <summary>
    /// Skip block quote marker.
    /// </summary>
    /// <param name="line">The line to skip block quote marker.</param>
    /// <param name="columnNumber">Column number of the start of <paramref name="line"/>.</param>
    /// <param name="length">Total length of leading spaces and block quote marker.</param>
    /// <returns>remaining of the line,
    /// and if no block quote marker was skipped, return <paramref name="line"/>.</returns>
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
    /// Append content of a block quote line to <paramref name="builder"/>,
    /// block quote mark should already be skipped.
    /// </summary>
    /// <param name="builder">The <see cref="StringBuilder"/> which content should be append to.</param>
    /// <param name="line">A block quote line with block quote mark skipped.</param>
    /// <param name="columnNumber">columnNumber of the start of the <paramref name="line"/>.</param>
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

    private static ReadOnlySpan<char> Skip(ReadOnlySpan<char> text,
                                           out BlockQuote? result,
                                           MarkdownBlock father,
                                           IEnumerable<IMarkdownBlockParser> blockParsers)
    {
        result = null;

        var line = TextUtils.ReadLine(text, out var remaining, out int columnNumber, out _);

        line = SkipBlockQuoteMarker(line, columnNumber, out int length);
        if (length is 0) return text;
        columnNumber += length;

        var contentBuilder = new StringBuilder();
        result = new BlockQuote();

        AppendBlockQuoteContent(contentBuilder, line, columnNumber);

        contentBuilder.Append('\n');

        while (true)
        {
            text = remaining;
            if (text.IsEmpty)
            {
                if (contentBuilder.Length is not 0)
                {
                    var content = contentBuilder.ToString();
                    MarkdownParser.ParseBlocks(content, result, blockParsers);
                }

                father.Children.Add(result);
                return text;
            }

            line = TextUtils.ReadLine(text, out remaining, out columnNumber, out _);
            line = SkipBlockQuoteMarker(line, columnNumber, out length);
            if (length != 0)
            {
                columnNumber += length;
                AppendBlockQuoteContent(contentBuilder, line, columnNumber);
                contentBuilder.Append('\n');
            }
            else // maybe continuation text
            {
                var content = contentBuilder.ToString();
                var tmpResult = new BlockQuote();
                MarkdownParser.ParseBlocks(content, tmpResult, blockParsers);

                if (tmpResult.LastChild is not Paragraph && !(tmpResult.LastChild is ContainerBlock container && Utils.IsNestedLastParagraph(container)))
                {
                    father.Children.Add(tmpResult);
                    return text;
                }
                else if (MarkdownParser.ParseBlock(ref text,
                                                   tmpResult,
                                                   blockParsers.Where(parser => parser is not IndentedCodeBlockParser))) // not continuation
                {
                    father.Children.Add(tmpResult);
                    father.Children.Add(tmpResult.LastChild);
                    tmpResult.Children.RemoveAt(tmpResult.Children.Count - 1);
                    return text;
                }
                else
                {
                    contentBuilder.Append(line.MarkAsParagraph());
                    contentBuilder.Append('\n');
                }
            }
        }
    }

    public bool TryReadAndParse(ref ReadOnlySpan<char> text,
                                MarkdownBlock father,
                                IEnumerable<IMarkdownBlockParser> blockParsers)
    {
        var remaining = Skip(text, out _, father, blockParsers);
        if (remaining == text)
        {
            return false;
        }

        text = remaining;
        return true;
    }
}
