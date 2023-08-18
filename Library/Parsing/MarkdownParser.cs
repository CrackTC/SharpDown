using CrackTC.SharpDown.Parsing.Block;
using CrackTC.SharpDown.Parsing.Block.Container;
using CrackTC.SharpDown.Parsing.Block.Leaf;
using CrackTC.SharpDown.Parsing.Inline.Leaf;
using CrackTC.SharpDown.Parsing.Inline.Nested;
using CrackTC.SharpDown.Structure;
using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Leaf;
using CrackTC.SharpDown.Structure.Inline;
using CrackTC.SharpDown.Structure.Inline.Leaf;
using CrackTC.SharpDown.Structure.Inline.Nested;

namespace CrackTC.SharpDown.Parsing;

public static class MarkdownParser
{
    internal static bool ParseBlock(ref ReadOnlySpan<char> text,
        MarkdownBlock father,
        IEnumerable<IMarkdownBlockParser> blockParsers)
    {
        foreach (var blockParser in blockParsers)
            if (blockParser.TryReadAndParse(ref text, father, blockParsers))
                return true;

        return false;
    }

    internal static void ParseBlocks(ReadOnlySpan<char> text,
        MarkdownBlock father,
        IEnumerable<IMarkdownBlockParser> blockParsers)
    {
        // parse blocks
        var lastIsParagraph = father.LastChild is Paragraph;

        while (text.IsEmpty is false)
            if (ParseBlock(ref text, father, blockParsers))
            {
                lastIsParagraph = false;
            }
            else if (lastIsParagraph)
            {
                var previousParagraph = father.LastChild as Paragraph;
                father.LastChild = new Paragraph(previousParagraph!.Content
                                                 + '\n'
                                                 + TextUtils.ReadLine(text, out text, out _, out _).ToString());
            }
            else
            {
                lastIsParagraph = true;
                father.Children.Add(new Paragraph(TextUtils.ReadLine(text, out text, out _, out _).ToString()));
            }
    }

    private static MarkdownRoot Parse(ReadOnlySpan<char> text,
        IEnumerable<IMarkdownBlockParser> blockParsers,
        IEnumerable<IMarkdownLeafInlineParser> leafInlineParsers)
    {
        var root = new MarkdownRoot();
        ParseBlocks(text, root, blockParsers);
        Utils.CombineListItems(root);
        var definitions = Utils.GetLinkReferenceDefinitions(root);
        root.ParseInline(leafInlineParsers, definitions);
        return root;
    }

    public static MarkdownRoot Parse(ReadOnlySpan<char> text)
    {
        var blockParsers = new IMarkdownBlockParser[]
        {
            new ThematicBreakParser(),
            new BlockQuoteParser(),
            new ListItemParser(),
            new BlankLineParser(),
            new AtxHeadingParser(),
            new FencedCodeBlockParser(),
            new HtmlBlockParser(),
            new IndentedCodeBlockParser(),
            new LinkReferenceDefinitionParser(),
            new SetextHeadingParser(),
            new TableParser(),
            new MathBlockParser()
        };

        var leafInlineParsers = new IMarkdownLeafInlineParser[]
        {
            new CodeSpanParser(),
            new AutolinkParser(),
            new HtmlTagParser(),
            new WikiLinkParser(),
            new EmbeddedFileParser(),
            new MathSpanParser(),
            new HardLineBreakParser(),
            new SoftLineBreakParser()
        };

        return Parse(text, blockParsers, leafInlineParsers);
    }

    private static void ProcessTilde(ReadOnlySpan<char> text,
        ref int i,
        ref int textBegin,
        LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
        LinkedList<DelimiterNode> delimiterStack)
    {
        int tildeCount = 1;
        while (i + tildeCount < text.Length && text[i + tildeCount] == '~') tildeCount++;
        if (tildeCount > 2)
        {
            i += tildeCount - 1;
            return;
        }
        Utils.AppendTextRange(text, textBegin, i, textNodes);
        textNodes.AddLast((i, new Text(tildeCount == 1 ? "~" : "~~")));
        textBegin = i + tildeCount;
        i = textBegin - 1;
        var node = new DelimiterNode
        {
            Number = tildeCount,
            TextNode = textNodes.Last!,
            Type = DelimiterType.Tilde
        };
        delimiterStack.AddLast(node);
    }


    private static void ProcessStar(ReadOnlySpan<char> text,
        ref int i,
        ref int textBegin,
        LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
        LinkedList<DelimiterNode> delimiterStack)
    {
        var starCount = 1;
        int j;
        for (j = i + 1; j < text.Length; j++)
        {
            if (text[j] is not '*') break;
            starCount++;
        }

        var followedByWhitespace = j == text.Length || text[j].IsUnicodeWhiteSpace() || text[j].IsLineEnding();
        var followedByPunctuation = j != text.Length && text[j].IsUnicodePunctuation();
        var precededByWhitespace = i == 0 || text[i - 1].IsUnicodeWhiteSpace() || text[i - 1].IsLineEnding();
        var precededByPunctuation = i != 0 && text[i - 1].IsUnicodePunctuation();

        var isLeftFlankingDelimiterRun = !followedByWhitespace
                                         && (!followedByPunctuation || precededByWhitespace || precededByPunctuation);

        var isRightFlankingDelimiterRun = !precededByWhitespace
                                          && (!precededByPunctuation || followedByWhitespace || followedByPunctuation);

        if (!isLeftFlankingDelimiterRun && !isRightFlankingDelimiterRun)
        {
            i = j - 1;
            return;
        }

        Utils.AppendTextRange(text, textBegin, i, textNodes);

        textNodes.AddLast((i, new Text(text[i..j].ToString())));
        textBegin = j;
        i = textBegin - 1;

        var node = new DelimiterNode
        {
            Number = starCount,
            TextNode = textNodes.Last!,
            Type = (isLeftFlankingDelimiterRun ? DelimiterType.Open : DelimiterType.None)
                   | (isRightFlankingDelimiterRun ? DelimiterType.Closing : DelimiterType.None)
                   | DelimiterType.Star
        };
        delimiterStack.AddLast(node);
    }

    private static void ProcessUnderscore(ReadOnlySpan<char> text,
        ref int i,
        ref int textBegin,
        LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
        LinkedList<DelimiterNode> delimiterStack)
    {
        var underscoreCount = 1;
        int j;
        for (j = i + 1; j < text.Length; j++)
        {
            if (text[j] is not '_') break;

            underscoreCount++;
        }

        var followedByWhitespace = j == text.Length || text[j].IsUnicodeWhiteSpace() || text[j].IsLineEnding();
        var followedByPunctuation = j != text.Length && text[j].IsUnicodePunctuation();
        var precededByWhitespace = i == 0 || text[i - 1].IsUnicodeWhiteSpace() || text[i - 1].IsLineEnding();
        var precededByPunctuation = i != 0 && text[i - 1].IsUnicodePunctuation();

        var isLeftFlankingDelimiterRun = !followedByWhitespace
                                         && (!followedByPunctuation || precededByWhitespace || precededByPunctuation);

        var isRightFlankingDelimiterRun = !precededByWhitespace
                                          && (!precededByPunctuation || followedByWhitespace || followedByPunctuation);

        if (!isLeftFlankingDelimiterRun && !isRightFlankingDelimiterRun)
        {
            i = j - 1;
            return;
        }

        Utils.AppendTextRange(text, textBegin, i, textNodes);

        textNodes.AddLast((i, new Text(text[i..j].ToString())));
        textBegin = j;
        i = textBegin - 1;

        var node = new DelimiterNode
        {
            Number = underscoreCount,
            TextNode = textNodes.Last!,
            Type = (isLeftFlankingDelimiterRun && (!isRightFlankingDelimiterRun || precededByPunctuation)
                       ? DelimiterType.Open
                       : DelimiterType.None)
                   | (isRightFlankingDelimiterRun && (!isLeftFlankingDelimiterRun || followedByPunctuation)
                       ? DelimiterType.Closing
                       : DelimiterType.None)
                   | DelimiterType.Underscore
        };
        delimiterStack.AddLast(node);
    }


    private static void ProcessLinkStart(ReadOnlySpan<char> text,
        ref int i,
        ref int textBegin,
        LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
        LinkedList<DelimiterNode> delimiterStack)
    {
        Utils.AppendTextRange(text, textBegin, i, textNodes);

        textNodes.AddLast((i, new Text("[")));
        textBegin = i + 1;

        var node = new DelimiterNode
        {
            Number = 1,
            TextNode = textNodes.Last!,
            Type = DelimiterType.Link | DelimiterType.Active
        };
        delimiterStack.AddLast(node);
    }

    private static void ProcessImageStart(ReadOnlySpan<char> text,
        ref int i,
        ref int textBegin,
        LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
        LinkedList<DelimiterNode> delimiterStack)
    {
        if (i >= text.Length - 1 || text[i + 1] is not '[') return;
        Utils.AppendTextRange(text, textBegin, i, textNodes);

        textNodes.AddLast((i, new Text("![")));
        textBegin = i + 2;
        i++;

        var node = new DelimiterNode
        {
            Number = 1,
            TextNode = textNodes.Last!,
            Type = DelimiterType.Image | DelimiterType.Active
        };
        delimiterStack.AddLast(node);
    }

    private static Dictionary<string, LinkReferenceDefinition> GetReferenceDictionary(
        IEnumerable<LinkReferenceDefinition> definitions)
    {
        var result = new Dictionary<string, LinkReferenceDefinition>(StringComparer.InvariantCultureIgnoreCase);
        foreach (var definition in definitions.Reverse()) result[definition.Label.NormalizeLabel()] = definition;
        return result;
    }

    private static bool ProcessLeafInline(ReadOnlySpan<char> text,
        ref int i,
        ref int textBegin,
        LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
        IEnumerable<IMarkdownLeafInlineParser> parsers)
    {
        foreach (var parser in parsers)
        {
            var length = parser.TryParse(text[i..], out var inline);
            if (length is 0) continue;
            Utils.AppendTextRange(text, textBegin, i, textNodes);

            textNodes.AddLast((i, inline!));
            textBegin = i + length;
            i = textBegin - 1;
            return true;
        }

        return false;
    }

    private static bool ProcessInlineLink(ReadOnlySpan<char> text,
        ref int i,
        ref int textBegin,
        LinkedListNode<DelimiterNode> openDelimiterNode,
        LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
        LinkedList<DelimiterNode> delimiterStack)
    {
        var remaining = text[(i + 2)..];

        if (!remaining.TryRemoveTagInnerSpaces()
            || !remaining.TryReadLinkDestination(out var destination)) return false;


        var tmp = remaining;
        remaining.TryRemoveTagInnerSpaces();

        string? title = null;
        if (remaining == tmp
            || !remaining.TryReadLinkTitle(out title)
            || !remaining.TryRemoveTagInnerSpaces()
            || !remaining.StartsWith(")"))
        {
            remaining = tmp;
            if (!remaining.TryRemoveTagInnerSpaces() || !remaining.StartsWith(")")) return false;
        }

        Utils.AppendTextRange(text, textBegin, i, textNodes);

        remaining = remaining[1..];
        textBegin = text.Length - remaining.Length;
        i = textBegin - 1;

        ProcessStrikethrough(textNodes, delimiterStack, openDelimiterNode);
        ProcessEmphasis(textNodes, delimiterStack, openDelimiterNode);
        Utils.RemoveToEnd(delimiterStack, openDelimiterNode.Next);

        var link = new Link(
            Utils.ElementsAfter(openDelimiterNode.Value.TextNode).Select(tuple => tuple.Inline).ToList(),
            destination,
            title ?? string.Empty);

        Utils.RemoveToEnd(textNodes, openDelimiterNode.Value.TextNode);
        textNodes.AddLast((openDelimiterNode.Value.TextNode.Value.StartIndex, link));
        Utils.DeactivateLinks(openDelimiterNode);
        delimiterStack.Remove(openDelimiterNode);
        return true;
    }

    private static void ProcessInlineImage(ReadOnlySpan<char> text,
        ref int i,
        ref int textBegin,
        LinkedListNode<DelimiterNode> openDelimiterNode,
        LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
        LinkedList<DelimiterNode> delimiterStack)
    {
        var remaining = text[(i + 2)..];

        if (remaining.TryRemoveTagInnerSpaces() is false
            || remaining.TryReadLinkDestination(out var destination) is false)
        {
            delimiterStack.Remove(openDelimiterNode);
            return;
        }


        var tmp = remaining;
        remaining.TryRemoveTagInnerSpaces();

        string? title = null;
        if (remaining == tmp
            || remaining.TryReadLinkTitle(out title) is false
            || remaining.TryRemoveTagInnerSpaces() is false
            || remaining.StartsWith(")") is false)
        {
            remaining = tmp;
            if (remaining.TryRemoveTagInnerSpaces() is false
                || remaining.StartsWith(")") is false)
            {
                delimiterStack.Remove(openDelimiterNode);
                return;
            }
        }

        Utils.AppendTextRange(text, textBegin, i, textNodes);

        remaining = remaining[1..];
        textBegin = text.Length - remaining.Length;
        i = textBegin - 1;

        ProcessStrikethrough(textNodes, delimiterStack, openDelimiterNode);
        ProcessEmphasis(textNodes, delimiterStack, openDelimiterNode);
        Utils.RemoveToEnd(delimiterStack, openDelimiterNode.Next);

        var link = new Image(
            Utils.ElementsAfter(openDelimiterNode.Value.TextNode).Select(tuple => tuple.Inline).ToList(),
            destination,
            title ?? string.Empty);

        Utils.RemoveToEnd(textNodes, openDelimiterNode.Value.TextNode);
        textNodes.AddLast((openDelimiterNode.Value.TextNode.Value.StartIndex, link));
        delimiterStack.Remove(openDelimiterNode);
    }

    private static void ProcessFullReferenceLink(ReadOnlySpan<char> text,
        ref int i,
        ref int textBegin,
        LinkedListNode<DelimiterNode> openDelimiterNode,
        LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
        LinkedList<DelimiterNode> delimiterStack,
        IReadOnlyDictionary<string, LinkReferenceDefinition> definitionDictionary)
    {
        var remaining = text[(i + 1)..];

        if (remaining.TryReadLinkLabel(out var label) is false)
        {
            delimiterStack.Remove(openDelimiterNode);
            return;
        }

        label = label.NormalizeLabel();

        if (definitionDictionary.TryGetValue(label, out var definition) is false)
        {
            delimiterStack.Remove(openDelimiterNode);
            return;
        }

        Utils.AppendTextRange(text, textBegin, i, textNodes);

        textBegin = text.Length - remaining.Length;
        i = textBegin - 1;

        ProcessStrikethrough(textNodes, delimiterStack, openDelimiterNode);
        ProcessEmphasis(textNodes, delimiterStack, openDelimiterNode);
        Utils.RemoveToEnd(delimiterStack, openDelimiterNode.Next);

        var link = new Link(
            Utils.ElementsAfter(openDelimiterNode.Value.TextNode).Select(tuple => tuple.Inline).ToList(),
            definition.Destination,
            definition.Title);

        Utils.RemoveToEnd(textNodes, openDelimiterNode.Value.TextNode);
        textNodes.AddLast((openDelimiterNode.Value.TextNode.Value.StartIndex, link));
        Utils.DeactivateLinks(openDelimiterNode);
        delimiterStack.Remove(openDelimiterNode);
    }

    private static void ProcessFullReferenceImage(ReadOnlySpan<char> text,
        ref int i,
        ref int textBegin,
        LinkedListNode<DelimiterNode> openDelimiterNode,
        LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
        LinkedList<DelimiterNode> delimiterStack,
        IReadOnlyDictionary<string, LinkReferenceDefinition> definitionDictionary)
    {
        var remaining = text[(i + 1)..];

        if (remaining.TryReadLinkLabel(out var label) is false)
        {
            delimiterStack.Remove(openDelimiterNode);
            return;
        }

        label = label.NormalizeLabel();

        if (definitionDictionary.TryGetValue(label, out var definition) is false)
        {
            delimiterStack.Remove(openDelimiterNode);
            return;
        }

        Utils.AppendTextRange(text, textBegin, i, textNodes);

        textBegin = text.Length - remaining.Length;
        i = textBegin - 1;

        ProcessStrikethrough(textNodes, delimiterStack, openDelimiterNode);
        ProcessEmphasis(textNodes, delimiterStack, openDelimiterNode);
        Utils.RemoveToEnd(delimiterStack, openDelimiterNode.Next);

        var link = new Image(
            Utils.ElementsAfter(openDelimiterNode.Value.TextNode).Select(tuple => tuple.Inline).ToList(),
            definition.Destination,
            definition.Title);

        Utils.RemoveToEnd(textNodes, openDelimiterNode.Value.TextNode);
        textNodes.AddLast((openDelimiterNode.Value.TextNode.Value.StartIndex, link));
        delimiterStack.Remove(openDelimiterNode);
    }

    private static void ProcessCollapsedReferenceLink(ReadOnlySpan<char> text,
        ref int i,
        ref int textBegin,
        LinkedListNode<DelimiterNode> openDelimiterNode,
        LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
        LinkedList<DelimiterNode> delimiterStack,
        IReadOnlyDictionary<string, LinkReferenceDefinition> definitionDictionary)
    {
        var labelBegin = openDelimiterNode.Value.TextNode.Value.StartIndex;
        var label = text[(labelBegin + 1)..i].ToString().NormalizeLabel();

        if (definitionDictionary.TryGetValue(label, out var definition) is false)
        {
            delimiterStack.Remove(openDelimiterNode);
            return;
        }

        Utils.AppendTextRange(text, textBegin, i, textNodes);

        textBegin = i + 3;
        i = textBegin - 1;

        ProcessStrikethrough(textNodes, delimiterStack, openDelimiterNode);
        ProcessEmphasis(textNodes, delimiterStack, openDelimiterNode);
        Utils.RemoveToEnd(delimiterStack, openDelimiterNode.Next);

        var link = new Link(
            Utils.ElementsAfter(openDelimiterNode.Value.TextNode).Select(tuple => tuple.Inline).ToList(),
            definition.Destination,
            definition.Title);

        Utils.RemoveToEnd(textNodes, openDelimiterNode.Value.TextNode);
        textNodes.AddLast((openDelimiterNode.Value.TextNode.Value.StartIndex, link));
        Utils.DeactivateLinks(openDelimiterNode);
        delimiterStack.Remove(openDelimiterNode);
    }

    private static void ProcessCollapsedReferenceImage(ReadOnlySpan<char> text,
        ref int i,
        ref int textBegin,
        LinkedListNode<DelimiterNode> openDelimiterNode,
        LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
        LinkedList<DelimiterNode> delimiterStack,
        IReadOnlyDictionary<string, LinkReferenceDefinition> definitionDictionary)
    {
        var labelBegin = openDelimiterNode.Value.TextNode.Value.StartIndex;
        var label = text[(labelBegin + 2)..i].ToString().NormalizeLabel();

        if (definitionDictionary.TryGetValue(label, out var definition) is false)
        {
            delimiterStack.Remove(openDelimiterNode);
            return;
        }

        Utils.AppendTextRange(text, textBegin, i, textNodes);

        textBegin = i + 3;
        i = textBegin - 1;

        ProcessStrikethrough(textNodes, delimiterStack, openDelimiterNode);
        ProcessEmphasis(textNodes, delimiterStack, openDelimiterNode);
        Utils.RemoveToEnd(delimiterStack, openDelimiterNode.Next);

        var link = new Image(
            Utils.ElementsAfter(openDelimiterNode.Value.TextNode).Select(tuple => tuple.Inline).ToList(),
            definition.Destination,
            definition.Title);

        Utils.RemoveToEnd(textNodes, openDelimiterNode.Value.TextNode);
        textNodes.AddLast((openDelimiterNode.Value.TextNode.Value.StartIndex, link));
        delimiterStack.Remove(openDelimiterNode);
    }


    private static void ProcessShortcutReferenceLink(ReadOnlySpan<char> text,
        ref int i,
        ref int textBegin,
        LinkedListNode<DelimiterNode> openDelimiterNode,
        LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
        LinkedList<DelimiterNode> delimiterStack,
        IReadOnlyDictionary<string, LinkReferenceDefinition> definitionDictionary)
    {
        var labelBegin = openDelimiterNode.Value.TextNode.Value.StartIndex;
        var label = text[(labelBegin + 1)..i].ToString().NormalizeLabel();

        if (definitionDictionary.TryGetValue(label, out var definition) is false)
        {
            delimiterStack.Remove(openDelimiterNode);
            return;
        }

        Utils.AppendTextRange(text, textBegin, i, textNodes);

        textBegin = i + 1;
        i = textBegin - 1;

        ProcessStrikethrough(textNodes, delimiterStack, openDelimiterNode);
        ProcessEmphasis(textNodes, delimiterStack, openDelimiterNode);
        Utils.RemoveToEnd(delimiterStack, openDelimiterNode.Next);

        var link = new Link(
            Utils.ElementsAfter(openDelimiterNode.Value.TextNode).Select(tuple => tuple.Inline).ToList(),
            definition.Destination,
            definition.Title);

        Utils.RemoveToEnd(textNodes, openDelimiterNode.Value.TextNode);
        textNodes.AddLast((openDelimiterNode.Value.TextNode.Value.StartIndex, link));
        Utils.DeactivateLinks(openDelimiterNode);
        delimiterStack.Remove(openDelimiterNode);
    }

    private static void ProcessShortcutReferenceImage(ReadOnlySpan<char> text,
        ref int i,
        ref int textBegin,
        LinkedListNode<DelimiterNode> openDelimiterNode,
        LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
        LinkedList<DelimiterNode> delimiterStack,
        IReadOnlyDictionary<string, LinkReferenceDefinition> definitionDictionary)
    {
        var labelBegin = openDelimiterNode.Value.TextNode.Value.StartIndex;
        var label = text[(labelBegin + 2)..i].ToString().NormalizeLabel();

        if (definitionDictionary.TryGetValue(label, out var definition) is false)
        {
            delimiterStack.Remove(openDelimiterNode);
            return;
        }

        Utils.AppendTextRange(text, textBegin, i, textNodes);

        textBegin = i + 1;
        i = textBegin - 1;

        ProcessStrikethrough(textNodes, delimiterStack, openDelimiterNode);
        ProcessEmphasis(textNodes, delimiterStack, openDelimiterNode);
        Utils.RemoveToEnd(delimiterStack, openDelimiterNode.Next);

        var link = new Image(
            Utils.ElementsAfter(openDelimiterNode.Value.TextNode).Select(tuple => tuple.Inline).ToList(),
            definition.Destination, definition.Title);

        Utils.RemoveToEnd(textNodes, openDelimiterNode.Value.TextNode);
        textNodes.AddLast((openDelimiterNode.Value.TextNode.Value.StartIndex, link));
        delimiterStack.Remove(openDelimiterNode);
    }

    private static void ProcessLinkAndImage(ReadOnlySpan<char> text,
        ref int i,
        ref int textBegin,
        LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
        LinkedList<DelimiterNode> delimiterStack,
        IReadOnlyDictionary<string, LinkReferenceDefinition> definitionDictionary)
    {
        var currentNode = delimiterStack.Last;
        while (currentNode is not null)
        {
            var delimiterInfo = currentNode.Value;
            if (delimiterInfo.Type.HasFlag(DelimiterType.Link))
            {
                if (delimiterInfo.Type.HasFlag(DelimiterType.Active) is false)
                {
                    delimiterStack.Remove(currentNode);
                    return;
                }

                if (text[(i + 1)..].StartsWith("(")) // inline link
                {
                    if (!ProcessInlineLink(text, ref i, ref textBegin, currentNode, textNodes, delimiterStack))
                        ProcessShortcutReferenceLink(text, ref i, ref textBegin, currentNode, textNodes, delimiterStack,
                            definitionDictionary);
                    return;
                }

                if (text[(i + 1)..].StartsWith("[")) // collapsed or full reference link
                {
                    if (text[(i + 2)..].StartsWith("]"))
                        ProcessCollapsedReferenceLink(text, ref i, ref textBegin, currentNode, textNodes,
                            delimiterStack, definitionDictionary);
                    else
                        ProcessFullReferenceLink(text, ref i, ref textBegin, currentNode, textNodes, delimiterStack,
                            definitionDictionary);
                    return;
                }

                // shortcut reference link
                ProcessShortcutReferenceLink(text, ref i, ref textBegin, currentNode, textNodes, delimiterStack,
                    definitionDictionary);
                return;
            }

            if (delimiterInfo.Type.HasFlag(DelimiterType.Image))
            {
                if (delimiterInfo.Type.HasFlag(DelimiterType.Active) is false)
                {
                    delimiterStack.Remove(currentNode);
                    return;
                }

                if (text[(i + 1)..].StartsWith("("))
                {
                    ProcessInlineImage(text, ref i, ref textBegin, currentNode, textNodes, delimiterStack);
                    return;
                }

                if (text[(i + 1)..].StartsWith("["))
                {
                    if (text[(i + 2)..].StartsWith("]"))
                        ProcessCollapsedReferenceImage(text, ref i, ref textBegin, currentNode, textNodes,
                            delimiterStack, definitionDictionary);
                    else
                        ProcessFullReferenceImage(text, ref i, ref textBegin, currentNode, textNodes, delimiterStack,
                            definitionDictionary);
                    return;
                }

                ProcessShortcutReferenceImage(text, ref i, ref textBegin, currentNode, textNodes, delimiterStack,
                    definitionDictionary);
                return;
            }

            currentNode = currentNode.Previous;
        }
    }

    internal static void ParseInline(ReadOnlySpan<char> text,
        MarkdownBlock father,
        IEnumerable<IMarkdownLeafInlineParser> parsers,
        IEnumerable<LinkReferenceDefinition> definitions)
    {
        text = text.TrimTabAndSpace();
        var textNodes = new LinkedList<(int StartIndex, MarkdownInline Inline)>();
        var delimiterStack = new LinkedList<DelimiterNode>();
        var definitionDictionary = GetReferenceDictionary(definitions);

        var textBegin = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (ProcessLeafInline(text, ref i, ref textBegin, textNodes, parsers)) continue;

            // handle backslash escape
            if (text[i] is '\\' && i < text.Length - 1)
                if (text[i + 1].IsAsciiPunctuation())
                {
                    i++;
                    continue;
                }

            switch (text[i])
            {
                case '~':
                    ProcessTilde(text, ref i, ref textBegin, textNodes, delimiterStack);
                    continue;
                case '*':
                    ProcessStar(text, ref i, ref textBegin, textNodes, delimiterStack);
                    continue;
                case '_':
                    ProcessUnderscore(text, ref i, ref textBegin, textNodes, delimiterStack);
                    continue;
                case '[':
                    ProcessLinkStart(text, ref i, ref textBegin, textNodes, delimiterStack);
                    continue;
                case '!':
                    ProcessImageStart(text, ref i, ref textBegin, textNodes, delimiterStack);
                    continue;
                case ']':
                    ProcessLinkAndImage(text, ref i, ref textBegin, textNodes, delimiterStack, definitionDictionary);
                    continue;
            }
        }

        Utils.AppendTextRange(text, textBegin, text.Length, textNodes);

        ProcessStrikethrough(textNodes, delimiterStack, null);
        ProcessEmphasis(textNodes, delimiterStack, null);
        Utils.RemoveToEnd(delimiterStack, delimiterStack.First);

        foreach (var node in textNodes) father.Children.Add(node.Inline);
    }

    private static bool IsSameDelimiter(DelimiterNode node1, DelimiterNode node2)
    {
        const DelimiterType mask = DelimiterType.Star | DelimiterType.Underscore;
        return ((node1.Type ^ node2.Type) & mask) == 0;
    }

    private static void ProcessEmphasis(LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
        LinkedList<DelimiterNode> delimiterStack,
        LinkedListNode<DelimiterNode>? stackBottom)
    {
        var currentPosition = stackBottom == null ? delimiterStack.First : stackBottom.Next;
        var openersBottomStar = stackBottom;
        var openersBottomUnderscore = stackBottom;

        var multipleOfThree = false;

        while (currentPosition is not null)
        {
            if (currentPosition.Value.Type.HasFlag(DelimiterType.Closing) is false
                || (currentPosition.Value.Type.HasFlag(DelimiterType.Star) is false
                    && currentPosition.Value.Type.HasFlag(DelimiterType.Underscore) is false))
            {
                currentPosition = currentPosition.Next;
                continue;
            }

            var opener = currentPosition.Previous;
            ref var openersBottom = ref currentPosition.Value.Type.HasFlag(DelimiterType.Star)
                ? ref openersBottomStar
                : ref openersBottomUnderscore;

            while (opener != stackBottom && opener != openersBottom)
            {
                if (opener!.Value.Type.HasFlag(DelimiterType.Open) is false
                    || IsSameDelimiter(opener.Value, currentPosition.Value) is false)
                {
                    opener = opener.Previous;
                    continue;
                }


                if (opener.Value.Type.HasFlag(DelimiterType.Open | DelimiterType.Closing)
                    || currentPosition.Value.Type.HasFlag(DelimiterType.Open | DelimiterType.Closing))
                {
                    var lengthStart = ((Text)opener.Value.TextNode.Value.Inline).Content.Length;
                    var lengthEnd = ((Text)currentPosition.Value.TextNode.Value.Inline).Content.Length;
                    if ((lengthStart + lengthEnd) % 3 is 0 && (lengthStart % 3 is not 0 || lengthEnd % 3 is not 0))
                    {
                        opener = opener.Previous;
                        multipleOfThree = true;
                        continue;
                    }
                }

                // found
                var isStrong = opener.Value.Number >= 2 && currentPosition.Value.Number >= 2;

                var emphasis = new Emphasis(isStrong);

                {
                    var node = opener.Value.TextNode;
                    while (node.Next != currentPosition.Value.TextNode)
                    {
                        emphasis.Children.Add(node.Next!.Value.Inline);
                        textNodes.Remove(node.Next!);
                    }

                    textNodes.AddAfter(node, (0, emphasis));
                }

                while (opener.Next != currentPosition) delimiterStack.Remove(opener.Next!);

                opener.ValueRef.Number -= isStrong ? 2 : 1;
                currentPosition.ValueRef.Number -= isStrong ? 2 : 1;
                if (opener.Value.Number is 0)
                {
                    delimiterStack.Remove(opener);
                    textNodes.Remove(opener.Value.TextNode);
                }
                else
                {
                    ref var value = ref opener.Value.TextNode.ValueRef;
                    value.Inline = new Text(((Text)value.Inline).Content[(isStrong ? 2 : 1)..]);
                }

                if (currentPosition.Value.Number is 0)
                {
                    textNodes.Remove(currentPosition.Value.TextNode);
                    var tmp = currentPosition;
                    currentPosition = currentPosition.Next;
                    delimiterStack.Remove(tmp);
                }
                else
                {
                    ref var value = ref currentPosition.Value.TextNode.ValueRef;
                    value.Inline = new Text(((Text)value.Inline).Content[(isStrong ? 2 : 1)..]);
                }

                break;
            }

            if (opener != stackBottom && opener != openersBottom) continue; // not found

            if (!multipleOfThree) openersBottom = currentPosition!.Previous;
            multipleOfThree = false;

            var tmp2 = currentPosition!;
            currentPosition = currentPosition!.Next;

            if (tmp2.Value.Type.HasFlag(DelimiterType.Open) is false) delimiterStack.Remove(tmp2);
        }

        Utils.RemoveToEnd(delimiterStack, stackBottom == null ? delimiterStack.First : stackBottom.Next);
    }

    private static void ProcessStrikethrough(LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
        LinkedList<DelimiterNode> delimiterStack,
        LinkedListNode<DelimiterNode>? stackBottom)
    {
        var opener = stackBottom == null ? delimiterStack.First : stackBottom.Next;

        while (opener != null)
        {
            if (!opener.Value.Type.HasFlag(DelimiterType.Tilde))
            {
                opener = opener.Next;
                continue;
            }

            var closer = opener.Next;

            while (closer != null)
            {
                if (!closer.Value.Type.HasFlag(DelimiterType.Tilde) || closer.Value.Number != opener.Value.Number)
                {
                    closer = closer.Next;
                    continue;
                }

                var strikethrough = new Strikethrough();

                var node = opener.Value.TextNode;
                while (node.Next != closer.Value.TextNode)
                {
                    strikethrough.Children.Add(node.Next!.Value.Inline);
                    textNodes.Remove(node.Next!);
                }

                textNodes.AddAfter(node, (0, strikethrough));

                while (opener.Next != closer) delimiterStack.Remove(opener.Next!);

                textNodes.Remove(closer.Value.TextNode);
                delimiterStack.Remove(closer);
                break;
            }

            if (closer != null) textNodes.Remove(opener.Value.TextNode);
            var tmp = opener;
            opener = opener.Next;
            delimiterStack.Remove(tmp);
        }
    }

}
