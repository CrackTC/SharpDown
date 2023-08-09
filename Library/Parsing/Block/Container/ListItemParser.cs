using CrackTC.SharpDown.Parsing.Block.Leaf;
using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Container;
using CrackTC.SharpDown.Structure.Block.Leaf;
using System.Text;

namespace CrackTC.SharpDown.Parsing.Block.Container;

internal class ListItemParser : IMarkdownBlockParser
{
    public static int CountBulletListMarker(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
        {
            return 0;
        }

        return "-+*".Contains(text[0]) ? 1 : 0;
    }

    public static int CountOrderedListMarker(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty) return 0;

        if (char.IsAsciiDigit(text[0]) is false) return 0;

        int i;
        for (i = 1; i < text.Length; i++)
        {
            if (char.IsAsciiDigit(text[i]) is false) break;
            if (i == 9) return 0; // 10 digits is too many
        }

        if (i == text.Length || text[i] is not '.' and not ')') return 0;

        return i + 1;
    }

    private static bool TryAppendListItemContent(StringBuilder builder, ReadOnlySpan<char> line, int columnNumber, int indentation)
    {
        if (line.IsBlankLine())
        {
            builder.Append('\n');
            return true;
        }

        var (count, index, tabRemainingSpaces) = line.CountLeadingSpace(columnNumber, indentation);

        if (count < indentation) return false;

        builder.Append((columnNumber + indentation).GenerateHeading())
               .Append(TextUtils.Space, tabRemainingSpaces)
               .Append(line[index..])
               .Append('\n');
        return true;
    }

    private static ReadOnlySpan<char> Skip(ReadOnlySpan<char> text,
                                           MarkdownBlock father,
                                           IEnumerable<IMarkdownBlockParser> blockParsers)
    {
        var isOrdered = false;
        var number = 0;
        char sign;

        var line = TextUtils.ReadLine(text, out var remaining, out int columnNumber, out _);

        var (leadingSpaceCount, index, _) = line.CountLeadingSpace(columnNumber, 4); // rule 4
        if (leadingSpaceCount is 4) return text; // 4 spaces is too many

        int markCount = CountBulletListMarker(line[index..]);
        if (markCount is 0) // not unordered list item
        {
            isOrdered = true;
            markCount = CountOrderedListMarker(line[index..]);
            if (markCount is 0) return text; // not ordered list item

            sign = line[index + markCount - 1];
            number = int.Parse(line[index..][..(markCount - 1)]);
        }
        else sign = line[index];

        if (isOrdered && number != 1 && father.LastChild is Paragraph) return text;

        var contentBuilder = new StringBuilder();
        var result = new ListItem(sign, isOrdered, number);

        line = line[(index + markCount)..];
        columnNumber += leadingSpaceCount + markCount;
        int indentation = leadingSpaceCount + markCount;

        bool beginWithBlankLine = false;

        if (line.IsBlankLine()) // rule 3
        {
            beginWithBlankLine = true;
            indentation++;

            if (father.LastChild is Paragraph) return text; // empty list item cannot interrupt a paragraph.
        }
        else
        {
            (var separatorCount, index, var tabRemainingSpaces) = line.CountLeadingSpace(columnNumber, 5);
            if (separatorCount is 5) // rule 2
            {
                contentBuilder.Append((columnNumber + 1).GenerateHeading())
                              .Append(TextUtils.Space, tabRemainingSpaces + 4);
                indentation++;
            }
            else if (separatorCount is 0) return text;
            else // rule 1
            {
                contentBuilder.Append((columnNumber + separatorCount).GenerateHeading());
                indentation += separatorCount;
            }
        }

        contentBuilder.Append(line[index..]).Append('\n');

        bool firstIter = true;

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
                while (result.LastChild is BlankLine && result.Children.Count != 1)
                {
                    father.Children.Add(result.LastChild);
                    result.Children.RemoveAt(result.Children.Count - 1);
                }
                return text;
            }

            line = TextUtils.ReadLine(text, out remaining, out columnNumber, out _);

            if (beginWithBlankLine && firstIter && line.IsBlankLine())
            {
                result.Children.Add(new BlankLine());
                father.Children.Add(result);
                while (result.LastChild is BlankLine)
                {
                    father.Children.Add(result.LastChild);
                    result.Children.RemoveAt(result.Children.Count - 1);
                }
                return text;
            }

            firstIter = false;

            var tmpResult = new BlockQuote();

            if (!TryAppendListItemContent(contentBuilder, line, columnNumber, indentation))
            {
                var content = contentBuilder.ToString();
                MarkdownParser.ParseBlocks(content, tmpResult, blockParsers);

                if (tmpResult.LastChild is not Paragraph && !(tmpResult.LastChild is ContainerBlock container && Utils.IsNestedLastParagraph(container)))
                {
                    result.Children.AddRange(tmpResult.Children);
                    father.Children.Add(result);
                    while (result.LastChild is BlankLine && result.Children.Count != 1)
                    {
                        father.Children.Add(result.LastChild);
                        result.Children.RemoveAt(result.Children.Count - 1);
                    }
                    return text;
                }

                var tmpResult2 = new BlockQuote();

                if (MarkdownParser.ParseBlock(ref text, tmpResult2, blockParsers.Where(parser => parser is not IndentedCodeBlockParser)))
                {
                    result.Children.AddRange(tmpResult.Children);
                    father.Children.Add(result);
                    while (result.LastChild is BlankLine && result.Children.Count != 1)
                    {
                        father.Children.Add(result.LastChild);
                        result.Children.RemoveAt(result.Children.Count - 1);
                    }
                    father.Children.AddRange(tmpResult2.Children);
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
        var remaining = Skip(text, father, blockParsers);
        if (remaining == text) return false;

        text = remaining;
        return true;
    }
}