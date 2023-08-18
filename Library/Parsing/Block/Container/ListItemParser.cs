using System.Diagnostics.CodeAnalysis;
using System.Text;
using CrackTC.SharpDown.Parsing.Block.Leaf;
using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Container;
using CrackTC.SharpDown.Structure.Block.Leaf;

namespace CrackTC.SharpDown.Parsing.Block.Container;

internal class ListItemParser : IMarkdownBlockParser
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

    private static int CountBulletListMarker(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty) return 0;
        return "-+*".Contains(text[0]) ? 1 : 0;
    }

    private static int CountOrderedListMarker(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty) return 0;
        if (!char.IsAsciiDigit(text[0])) return 0;

        int i;
        for (i = 1; i < text.Length; i++)
        {
            if (!char.IsAsciiDigit(text[i])) break;
            if (i == 9) return 0; // 10 digits is too many
        }

        if (i == text.Length || (text[i] != '.' && text[i] != ')')) return 0;
        return i + 1;
    }

    private static bool TryAppendListItemContent(StringBuilder builder, ReadOnlySpan<char> line, int columnNumber,
        int indentation)
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

    private static bool TryGetEmptyListItem(ReadOnlySpan<char> line, [NotNullWhen(true)] out ListItem? item,
        out int markCount)
    {
        item = null;

        markCount = CountBulletListMarker(line);
        if (markCount != 0) // not unordered list item
        {
            item = new ListItem(line[0], false, 0);
            return true;
        }

        markCount = CountOrderedListMarker(line);
        if (markCount == 0) return false; // not ordered list item

        item = new ListItem(line[markCount - 1], true, int.Parse(line[..(markCount - 1)]));
        return true;
    }

    private static (bool Success, int Indentation, bool IsBlankLine) HandleFirstLine(ReadOnlySpan<char> line,
        int columnNumber, StringBuilder builder, MarkdownBlock father)
    {
        if (line.IsBlankLine()) return (father.LastChild is not Paragraph, 1, true);

        var (separatorCount, index, tabRemainingSpaces) = line.CountLeadingSpace(columnNumber, 5);
        line = line[index..];
        int indentation;
        switch (separatorCount)
        {
            case 0:
                return (false, 0, false);
            case 5: // rule 2
                indentation = 1;
                builder.Append((columnNumber + indentation).GenerateHeading());
                builder.Append(TextUtils.Space, tabRemainingSpaces + 4);
                break;
            default: // rule 1
                indentation = separatorCount;
                builder.Append((columnNumber + indentation).GenerateHeading());
                break;
        }

        builder.Append(line);
        return (true, indentation, false);
    }

    private static ReadOnlySpan<char> Skip(ReadOnlySpan<char> text,
        MarkdownBlock father,
        IEnumerable<IMarkdownBlockParser> parsers)
    {
        var line = TextUtils.ReadLine(text, out var remaining, out var columnNumber, out _);

        var (leadingSpaceCount, index, _) = line.CountLeadingSpace(columnNumber, 4); // rule 4
        if (leadingSpaceCount is 4) return text; // 4 spaces is too many
        line = line[index..];

        if (!TryGetEmptyListItem(line, out var result, out var markCount))
            return text;
        line = line[markCount..];

        if (result.IsOrdered && result.Number != 1 && father.LastChild is Paragraph) return text;

        columnNumber += leadingSpaceCount + markCount;
        var contentBuilder = new StringBuilder();
        var (success, indentation, isBlankLine) = HandleFirstLine(line, columnNumber, contentBuilder, father);
        if (!success) return text;
        if (isBlankLine && TextUtils.ReadLine(remaining, out _, out _, out _).IsBlankLine())
        {
            result.Children.Add(new BlankLine());
            father.Children.Add(result);
            return remaining;
        }

        indentation += leadingSpaceCount + markCount;
        contentBuilder.Append('\n');

        while (!remaining.IsEmpty)
        {
            text = remaining;

            line = TextUtils.ReadLine(text, out remaining, out columnNumber, out _);

            if (TryAppendListItemContent(contentBuilder, line, columnNumber, indentation)) continue;

            var tmpResult = new ListItem(result.Sign, result.IsOrdered, result.Number);
            MarkdownParser.ParseBlocks(contentBuilder.ToString(), tmpResult, parsers);

            if (!Utils.IsNestedLastParagraph(tmpResult))
            {
                father.Children.Add(tmpResult);
                if (tmpResult.LastChild is not BlankLine || tmpResult.Children.Count == 1) return text;
                father.Children.Add(tmpResult.LastChild);
                tmpResult.Children.RemoveAt(tmpResult.Children.Count - 1);
                return text;
            }

            var tmpResult2 = new ListItem(result.Sign, result.IsOrdered, result.Number);
            if (MarkdownParser.ParseBlock(ref text, tmpResult2,
                    parsers.Where(parser => parser is not IndentedCodeBlockParser)))
            {
                father.Children.Add(tmpResult);
                if (tmpResult.LastChild is BlankLine && tmpResult.Children.Count != 1)
                {
                    father.Children.Add(tmpResult.LastChild);
                    tmpResult.Children.RemoveAt(tmpResult.Children.Count - 1);
                }

                father.Children.AddRange(tmpResult2.Children);
                return text;
            }

            contentBuilder.Append(line.MarkAsParagraph());
            contentBuilder.Append('\n');
        }

        MarkdownParser.ParseBlocks(contentBuilder.ToString(), result, parsers);
        father.Children.Add(result);
        if (result.LastChild is not BlankLine || result.Children.Count == 1) return ReadOnlySpan<char>.Empty;
        father.Children.Add(result.LastChild);
        result.Children.RemoveAt(result.Children.Count - 1);
        return ReadOnlySpan<char>.Empty;
    }
}