﻿using System.Text;
using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Leaf;

namespace CrackTC.SharpDown.Parsing.Block.Leaf;

internal class SetextHeadingParser : IMarkdownBlockParser
{
    private const string ValidChars = "=-";

    public bool TryReadAndParse(ref ReadOnlySpan<char> text, MarkdownBlock father,
        IEnumerable<IMarkdownBlockParser> parsers)
    {
        if (father.LastChild is Paragraph) return false;

        var remaining = Skip(text, out var level, out var content, parsers);
        if (remaining == text) return false;

        text = remaining;
        father.Children.Add(new SetextHeading(level, content));
        return true;
    }

    private static ReadOnlySpan<char> SkipUnderline(ReadOnlySpan<char> text, out int level)
    {
        level = 0;

        var line = TextUtils.ReadLine(text, out var remaining, out var columnNumber, out _);

        var (count, index, _) = line.CountLeadingSpace(columnNumber, 4);
        if (count == 4) return text;

        char? validChar = null;
        var trailingSpace = false;
        while (index < line.Length)
        {
            var ch = line[index];
            index++;

            if (validChar == null)
            {
                if (!ValidChars.Contains(ch)) return text;
                validChar = ch;
            }
            else if (ch.IsSpace() || ch.IsTab())
            {
                trailingSpace = true;
            }
            else if (trailingSpace || ch != validChar)
            {
                return text;
            }
        }

        if (validChar == null) return text;
        level = ValidChars.IndexOf(validChar.Value) + 1;
        return remaining;
    }

    private static ReadOnlySpan<char> Skip(ReadOnlySpan<char> text, out int level, out string content,
        IEnumerable<IMarkdownBlockParser> parsers)
    {
        level = 0;
        content = string.Empty;
        if (SkipUnderline(text, out _) != text) return text;

        var line = TextUtils.ReadLine(text, out var remaining, out var columnNumber, out _);
        var (count, index, _) = line.CountLeadingSpace(columnNumber, 4);
        if (count == 4 || line.IsBlankLine()) return text;

        var headingBuilder = new StringBuilder();
        headingBuilder.Append(line[index..]);

        while (true)
        {
            if (MarkdownParser.ParseBlock(ref remaining, new BlankLine(),
                    parsers.Where(p =>
                        p is not SetextHeadingParser
                            and not TableParser
                            and not ThematicBreakParser))) return text;
            line = TextUtils.ReadLine(remaining, out remaining, out _, out var markedAsParagraph);

            if (line.IsBlankLine()) return text;
            if (!markedAsParagraph)
            {
                var tmp = SkipUnderline(line, out level);
                if (tmp != line)
                {
                    content = headingBuilder.ToString().AsSpan().TrimTabAndSpace().ToString();
                    return remaining;
                }
            }

            headingBuilder.Append('\n').Append(line);
        }
    }
}