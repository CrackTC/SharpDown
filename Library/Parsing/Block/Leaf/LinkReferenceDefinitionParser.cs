﻿using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Leaf;

namespace CrackTC.SharpDown.Parsing.Block.Leaf;

internal class LinkReferenceDefinitionParser : IMarkdownBlockParser
{
    public bool TryReadAndParse(ref ReadOnlySpan<char> text,
        MarkdownBlock father,
        IEnumerable<IMarkdownBlockParser> parsers)
    {
        if (father.LastChild is Paragraph) return false;

        var remaining = Skip(text, out var label, out var destination, out var title);
        if (remaining == text) return false;

        text = remaining;
        father.Children.Add(new LinkReferenceDefinition(label, destination, title));
        return true;
    }

    private static ReadOnlySpan<char> Skip(ReadOnlySpan<char> text,
        out string label,
        out string destination,
        out string title)
    {
        label = destination = title = string.Empty;

        var tmp = text;
        var columnNumber = tmp.ReadColumnNumber();
        var (count, index, _) = tmp.CountLeadingSpace(columnNumber, 4);
        if (count is 4) return text;
        tmp = tmp[index..];

        if (!tmp.TryReadLinkLabel(out label) || !tmp.StartsWith(":")) return text;
        tmp = tmp[1..];

        if (!tmp.TryRemoveTagInnerSpaces() || !tmp.TryReadLinkDestination(out destination)) return text;

        ReadOnlySpan<char> remaining;
        var tmp2 = tmp;

        if (!tmp.TryRemoveTagInnerSpaces() || tmp == tmp2 || !tmp.TryReadLinkTitle(out title) || !tmp.IsBlankLine())
            return !TextUtils.ReadLine(tmp2, out remaining, out _, out _).IsBlankLine() ? text : remaining;
        TextUtils.ReadLine(tmp, out remaining, out _, out _);
        return remaining;
    }
}