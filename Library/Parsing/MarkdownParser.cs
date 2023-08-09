using CrackTC.SharpDown.Core.Parsing.Block.Leaf;
using CrackTC.SharpDown.Core.Parsing.Inline.Leaf;
using CrackTC.SharpDown.Parsing.Block;
using CrackTC.SharpDown.Parsing.Block.Container;
using CrackTC.SharpDown.Parsing.Block.Leaf;
using CrackTC.SharpDown.Parsing.Inline.Leaf;
using CrackTC.SharpDown.Parsing.Inline.Nested;
using CrackTC.SharpDown.Structure;
using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Container;
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
        {
            if (blockParser.TryReadAndParse(ref text, father, blockParsers))
            {
                return true;
            }
        }

        return false;
    }

    internal static void ParseBlocks(ReadOnlySpan<char> text,
                             MarkdownBlock father,
                             IEnumerable<IMarkdownBlockParser> blockParsers)
    {
        // parse blocks
        bool lastIsParagraph = false;

        if (father.LastChild is Paragraph) lastIsParagraph = true;

        while (text.IsEmpty is false)
        {
            if (ParseBlock(ref text, father, blockParsers)) lastIsParagraph = false;
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
    }

    private static IEnumerable<LinkReferenceDefinition> GetLinkReferenceDefinitions(MarkdownBlock root)
    {
        return root switch
        {
            LinkReferenceDefinition definition => Enumerable.Repeat(definition, 1),
            ContainerBlock container => container.Children.SelectMany(child => GetLinkReferenceDefinitions((MarkdownBlock)child)),
            MarkdownRoot rt => rt.Children.SelectMany(child => GetLinkReferenceDefinitions((MarkdownBlock)child)),
            _ => Enumerable.Empty<LinkReferenceDefinition>(),
        };
    }

    private static void CombineListItems(MarkdownBlock root)
    {
        var children = root.Children;
        for (int i = 0; i < children.Count; i++) if (children[i] is ContainerBlock block) CombineListItems(block);
        var newChildren = new List<MarkdownNode>();

        List list = null!;
        bool listAssigned = false;
        bool meetBlankLine = false;
        bool markedAsLoose = false;
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is ListItem listItem)
            {
                if (!listAssigned) list = new List() { Sign = listItem.Sign, Number = listItem.Number, IsOrdered = listItem.IsOrdered };
                else if (!ListItem.IsSameType((ListItem)list.Children[0], listItem))
                {
                    list.IsLoose = markedAsLoose || ListParser.IsListLoose(list);
                    newChildren.Add(list);

                    list = new List() { Sign = listItem.Sign, Number = listItem.Number, IsOrdered = listItem.IsOrdered };
                }
                else if (meetBlankLine)
                {
                    markedAsLoose = true;
                }

                listAssigned = true;
                list.Children.Add(listItem);
            }
            else if (children[i] is not BlankLine)
            {
                if (listAssigned)
                {
                    listAssigned = false;
                    list.IsLoose = markedAsLoose || ListParser.IsListLoose(list);
                    markedAsLoose = false;
                    newChildren.Add(list);
                }
                meetBlankLine = false;
                newChildren.Add(children[i]);
            }
            else
            {
                if (listAssigned)
                    meetBlankLine = true;
                newChildren.Add(new BlankLine());
            }
        }

        if (listAssigned)
        {
            list.IsLoose = markedAsLoose || ListParser.IsListLoose(list);
            newChildren.Add(list);
        }

        root.Children = newChildren;
    }

    private static MarkdownRoot Parse(ReadOnlySpan<char> text,
                                     IEnumerable<IMarkdownBlockParser> blockParsers,
                                     IEnumerable<IMarkdownLeafInlineParser> leafInlineParsers)
    {
        var root = new MarkdownRoot();
        ParseBlocks(text, root, blockParsers);
        CombineListItems(root);
        var definitions = GetLinkReferenceDefinitions(root);
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
        };

        var leafInlineParsers = new IMarkdownLeafInlineParser[]
        {
            new CodeSpanParser(),
            new AutolinkParser(),
            new HtmlTagParser(),
            new HardLineBreakParser(),
            new SoftLineBreakParser()
        };

        return Parse(text, blockParsers, leafInlineParsers);
    }

    private static void ProcessStar(ReadOnlySpan<char> text,
                                   ref int i,
                                   ref int textBegin,
                                   LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
                                   LinkedList<DelimiterNode> delimiterStack)
    {
        int starCount = 1;
        int j;
        for (j = i + 1; j < text.Length; j++)
        {
            if (text[j] is '*')
            {
                starCount++;
            }
            else
            {
                break;
            }
        }

        bool followedByWhitespace = j == text.Length || text[j].IsUnicodeWhiteSpace() || text[j].IsLineEnding();
        bool followedByPunctation = j != text.Length && text[j].IsUnicodePunctuation();
        bool precededByWhitespace = i == 0 || text[i - 1].IsUnicodeWhiteSpace() || text[i - 1].IsLineEnding();
        bool precededByPunctation = i != 0 && text[i - 1].IsUnicodePunctuation();

        bool isLeftFlankingDelimiterRun = !followedByWhitespace
                                          && (!followedByPunctation || precededByWhitespace || precededByPunctation);

        bool isRightFlankingDelimiterRun = !precededByWhitespace
                                           && (!precededByPunctation || followedByWhitespace || followedByPunctation);

        if (!isLeftFlankingDelimiterRun && !isRightFlankingDelimiterRun)
        {
            i = j - 1;
            return;
        }
        else
        {
            if (textBegin != i)
            {
                textNodes.AddLast((textBegin, new Text(text[textBegin..i].ToString())));
            }

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
    }

    private static void ProcessUnderscore(ReadOnlySpan<char> text,
                                         ref int i,
                                         ref int textBegin,
                                         LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
                                         LinkedList<DelimiterNode> delimiterStack)
    {
        int underscoreCount = 1;
        int j;
        for (j = i + 1; j < text.Length; j++)
        {
            if (text[j] is '_')
            {
                underscoreCount++;
            }
            else
            {
                break;
            }
        }

        bool followedByWhitespace = j == text.Length || text[j].IsUnicodeWhiteSpace() || text[j].IsLineEnding();
        bool followedByPunctation = j != text.Length && text[j].IsUnicodePunctuation();
        bool precededByWhitespace = i == 0 || text[i - 1].IsUnicodeWhiteSpace() || text[i - 1].IsLineEnding();
        bool precededByPunctation = i != 0 && text[i - 1].IsUnicodePunctuation();

        bool isLeftFlankingDelimiterRun = !followedByWhitespace
                                          && (!followedByPunctation || precededByWhitespace || precededByPunctation);

        bool isRightFlankingDelimiterRun = !precededByWhitespace
                                           && (!precededByPunctation || followedByWhitespace || followedByPunctation);

        if (!isLeftFlankingDelimiterRun && !isRightFlankingDelimiterRun)
        {
            i = j - 1;
            return;
        }
        else
        {
            if (textBegin != i)
            {
                textNodes.AddLast((textBegin, new Text(text[textBegin..i].ToString())));
            }

            textNodes.AddLast((i, new Text(text[i..j].ToString())));
            textBegin = j;
            i = textBegin - 1;

            var node = new DelimiterNode
            {
                Number = underscoreCount,
                TextNode = textNodes.Last!,
                Type = (isLeftFlankingDelimiterRun && (!isRightFlankingDelimiterRun || precededByPunctation)
                       ? DelimiterType.Open
                       : DelimiterType.None)

                     | (isRightFlankingDelimiterRun && (!isLeftFlankingDelimiterRun || followedByPunctation)
                       ? DelimiterType.Closing
                       : DelimiterType.None)

                     | DelimiterType.Underscore
            };
            delimiterStack.AddLast(node);
        }
    }


    private static void ProcessLinkStart(ReadOnlySpan<char> text,
                                       ref int i,
                                       ref int textBegin,
                                       LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
                                       LinkedList<DelimiterNode> delimiterStack)
    {
        if (textBegin != i)
        {
            textNodes.AddLast((textBegin, new Text(text[textBegin..i].ToString())));
        }

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
        if (i < text.Length - 1 && text[i + 1] is '[')
        {
            if (textBegin != i)
            {
                textNodes.AddLast((textBegin, new Text(text[textBegin..i].ToString())));
            }

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
    }

    private static Dictionary<string, LinkReferenceDefinition> GetReferenceDictionary(IEnumerable<LinkReferenceDefinition> definitions)
    {
        var result = new Dictionary<string, LinkReferenceDefinition>(StringComparer.InvariantCultureIgnoreCase);
        foreach (var definition in definitions.Reverse())
        {
            result[definition.Label.NormalizeLabel()] = definition;
        }
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
            int length = parser.TryReadAndParse(text[i..], out var inline);
            if (length is not 0)
            {
                if (textBegin != i)
                {
                    textNodes.AddLast((textBegin, new Text(text[textBegin..i].ToString())));
                }

                textNodes.AddLast((i, inline!));
                textBegin = i + length;
                i = textBegin - 1;
                return true;
            }
        }
        return false;
    }

    private static IEnumerable<T> ElementsAfter<T>(LinkedListNode<T> node)
    {
        while (node.Next is not null)
        {
            node = node.Next;
            yield return node.Value;
        }
    }

    private static void RemoveToEnd<T>(LinkedList<T> list, LinkedListNode<T>? node)
    {
        while (node is not null)
        {
            var tmp = node;
            node = node.Next;
            list.Remove(tmp);
        }
    }

    private static void DeactiveLinks(LinkedListNode<DelimiterNode> node)
    {
        while (node.Previous is not null)
        {
            if (node.Previous.Value.Type.HasFlag(DelimiterType.Link))
            {
                node.Previous.ValueRef.Type &= ~DelimiterType.Active;
            }

            node = node.Previous;
        }
    }

    private static bool ProcessInlineLink(ReadOnlySpan<char> text,
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
            return false;
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
                return false;
            }
        }

        if (textBegin != i)
        {
            textNodes.AddLast((textBegin, new Text(text[textBegin..i].ToString())));
        }

        remaining = remaining[1..];
        textBegin = text.Length - remaining.Length;
        i = textBegin - 1;

        ProcessEmphasis(textNodes, delimiterStack, openDelimiterNode);

        var link = new Link(ElementsAfter(openDelimiterNode.Value.TextNode).Select(tuple => tuple.Inline).ToList(),
                            destination,
                            title ?? string.Empty);

        RemoveToEnd(textNodes, openDelimiterNode.Value.TextNode);
        textNodes.AddLast((openDelimiterNode.Value.TextNode.Value.StartIndex, link));
        DeactiveLinks(openDelimiterNode);
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

        if (textBegin != i)
        {
            textNodes.AddLast((textBegin, new Text(text[textBegin..i].ToString())));
        }

        remaining = remaining[1..];
        textBegin = text.Length - remaining.Length;
        i = textBegin - 1;

        ProcessEmphasis(textNodes, delimiterStack, openDelimiterNode);

        var link = new Image(ElementsAfter(openDelimiterNode.Value.TextNode).Select(tuple => tuple.Inline).ToList(),
                            destination,
                            title ?? string.Empty);

        RemoveToEnd(textNodes, openDelimiterNode.Value.TextNode);
        textNodes.AddLast((openDelimiterNode.Value.TextNode.Value.StartIndex, link));
        delimiterStack.Remove(openDelimiterNode);
    }

    private static void ProcessFullReferenceLink(ReadOnlySpan<char> text,
                                                 ref int i,
                                                 ref int textBegin,
                                                 LinkedListNode<DelimiterNode> openDelimiterNode,
                                                 LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
                                                 LinkedList<DelimiterNode> delimiterStack,
                                                 Dictionary<string, LinkReferenceDefinition> definitionDictionary)
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

        if (textBegin != i)
        {
            textNodes.AddLast((textBegin, new Text(text[textBegin..i].ToString())));
        }

        textBegin = text.Length - remaining.Length;
        i = textBegin - 1;

        ProcessEmphasis(textNodes, delimiterStack, openDelimiterNode);

        var link = new Link(ElementsAfter(openDelimiterNode.Value.TextNode).Select(tuple => tuple.Inline).ToList(),
                            definition.Destination,
                            definition.Title);

        RemoveToEnd(textNodes, openDelimiterNode.Value.TextNode);
        textNodes.AddLast((openDelimiterNode.Value.TextNode.Value.StartIndex, link));
        DeactiveLinks(openDelimiterNode);
        delimiterStack.Remove(openDelimiterNode);
    }

    private static void ProcessFullReferenceImage(ReadOnlySpan<char> text,
                                                 ref int i,
                                                 ref int textBegin,
                                                 LinkedListNode<DelimiterNode> openDelimiterNode,
                                                 LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
                                                 LinkedList<DelimiterNode> delimiterStack,
                                                 Dictionary<string, LinkReferenceDefinition> definitionDictionary)
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

        if (textBegin != i)
        {
            textNodes.AddLast((textBegin, new Text(text[textBegin..i].ToString())));
        }

        textBegin = text.Length - remaining.Length;
        i = textBegin - 1;

        ProcessEmphasis(textNodes, delimiterStack, openDelimiterNode);

        var link = new Image(ElementsAfter(openDelimiterNode.Value.TextNode).Select(tuple => tuple.Inline).ToList(),
                             definition.Destination,
                             definition.Title);

        RemoveToEnd(textNodes, openDelimiterNode.Value.TextNode);
        textNodes.AddLast((openDelimiterNode.Value.TextNode.Value.StartIndex, link));
        delimiterStack.Remove(openDelimiterNode);
    }

    private static void ProcessCollapsedReferenceLink(ReadOnlySpan<char> text,
                                                 ref int i,
                                                 ref int textBegin,
                                                 LinkedListNode<DelimiterNode> openDelimiterNode,
                                                 LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
                                                 LinkedList<DelimiterNode> delimiterStack,
                                                 Dictionary<string, LinkReferenceDefinition> definitionDictionary)
    {
        int labelBegin = openDelimiterNode.Value.TextNode.Value.StartIndex;
        string label = text[(labelBegin + 1)..i].ToString().NormalizeLabel();

        if (definitionDictionary.TryGetValue(label, out var definition) is false)
        {
            delimiterStack.Remove(openDelimiterNode);
            return;
        }

        if (textBegin != i)
        {
            textNodes.AddLast((textBegin, new Text(text[textBegin..i].ToString())));
        }

        textBegin = i + 3;
        i = textBegin - 1;

        ProcessEmphasis(textNodes, delimiterStack, openDelimiterNode);

        var link = new Link(ElementsAfter(openDelimiterNode.Value.TextNode).Select(tuple => tuple.Inline).ToList(),
                            definition.Destination,
                            definition.Title);

        RemoveToEnd(textNodes, openDelimiterNode.Value.TextNode);
        textNodes.AddLast((openDelimiterNode.Value.TextNode.Value.StartIndex, link));
        DeactiveLinks(openDelimiterNode);
        delimiterStack.Remove(openDelimiterNode);
    }

    private static void ProcessCollapsedReferenceImage(ReadOnlySpan<char> text,
                                                 ref int i,
                                                 ref int textBegin,
                                                 LinkedListNode<DelimiterNode> openDelimiterNode,
                                                 LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
                                                 LinkedList<DelimiterNode> delimiterStack,
                                                 Dictionary<string, LinkReferenceDefinition> definitionDictionary)
    {
        int labelBegin = openDelimiterNode.Value.TextNode.Value.StartIndex;
        string label = text[(labelBegin + 2)..i].ToString().NormalizeLabel();

        if (definitionDictionary.TryGetValue(label, out var definition) is false)
        {
            delimiterStack.Remove(openDelimiterNode);
            return;
        }

        if (textBegin != i)
        {
            textNodes.AddLast((textBegin, new Text(text[textBegin..i].ToString())));
        }

        textBegin = i + 3;
        i = textBegin - 1;

        ProcessEmphasis(textNodes, delimiterStack, openDelimiterNode);

        var link = new Image(ElementsAfter(openDelimiterNode.Value.TextNode).Select(tuple => tuple.Inline).ToList(),
                             definition.Destination,
                             definition.Title);

        RemoveToEnd(textNodes, openDelimiterNode.Value.TextNode);
        textNodes.AddLast((openDelimiterNode.Value.TextNode.Value.StartIndex, link));
        delimiterStack.Remove(openDelimiterNode);
    }


    private static void ProcessShortcutReferenceLink(ReadOnlySpan<char> text,
                                                     ref int i,
                                                     ref int textBegin,
                                                     LinkedListNode<DelimiterNode> openDelimiterNode,
                                                     LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
                                                     LinkedList<DelimiterNode> delimiterStack,
                                                     Dictionary<string, LinkReferenceDefinition> definitionDictionary)
    {
        int labelBegin = openDelimiterNode.Value.TextNode.Value.StartIndex;
        string label = text[(labelBegin + 1)..i].ToString().NormalizeLabel();

        if (definitionDictionary.TryGetValue(label, out var definition) is false)
        {
            delimiterStack.Remove(openDelimiterNode);
            return;
        }

        if (textBegin != i)
        {
            textNodes.AddLast((textBegin, new Text(text[textBegin..i].ToString())));
        }

        textBegin = i + 1;
        i = textBegin - 1;

        ProcessEmphasis(textNodes, delimiterStack, openDelimiterNode);

        var link = new Link(ElementsAfter(openDelimiterNode.Value.TextNode).Select(tuple => tuple.Inline).ToList(),
                            definition.Destination,
                            definition.Title);

        RemoveToEnd(textNodes, openDelimiterNode.Value.TextNode);
        textNodes.AddLast((openDelimiterNode.Value.TextNode.Value.StartIndex, link));
        DeactiveLinks(openDelimiterNode);
        delimiterStack.Remove(openDelimiterNode);
    }

    private static void ProcessShortcutReferenceImage(ReadOnlySpan<char> text,
                                                      ref int i,
                                                      ref int textBegin,
                                                      LinkedListNode<DelimiterNode> openDelimiterNode,
                                                      LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
                                                      LinkedList<DelimiterNode> delimiterStack,
                                                      Dictionary<string, LinkReferenceDefinition> definitionDictionary)
    {
        int labelBegin = openDelimiterNode.Value.TextNode.Value.StartIndex;
        string label = text[(labelBegin + 2)..i].ToString().NormalizeLabel();

        if (definitionDictionary.TryGetValue(label, out var definition) is false)
        {
            delimiterStack.Remove(openDelimiterNode);
            return;
        }

        if (textBegin != i)
        {
            textNodes.AddLast((textBegin, new Text(text[textBegin..i].ToString())));
        }

        textBegin = i + 1;
        i = textBegin - 1;

        ProcessEmphasis(textNodes, delimiterStack, openDelimiterNode);

        var link = new Image(ElementsAfter(openDelimiterNode.Value.TextNode).Select(tuple => tuple.Inline).ToList(),
                             definition.Destination, definition.Title);

        RemoveToEnd(textNodes, openDelimiterNode.Value.TextNode);
        textNodes.AddLast((openDelimiterNode.Value.TextNode.Value.StartIndex, link));
        delimiterStack.Remove(openDelimiterNode);
    }

    private static void ProcessLinkAndImage(ReadOnlySpan<char> text,
                                   ref int i,
                                   ref int textBegin,
                                   LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes,
                                   LinkedList<DelimiterNode> delimiterStack,
                                   Dictionary<string, LinkReferenceDefinition> definitionDictionary)
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
                        ProcessShortcutReferenceLink(text, ref i, ref textBegin, currentNode, textNodes, delimiterStack, definitionDictionary);
                    return;
                }

                if (text[(i + 1)..].StartsWith("[")) // collapsed or full reference link
                {
                    if (text[(i + 2)..].StartsWith("]"))
                    {
                        ProcessCollapsedReferenceLink(text, ref i, ref textBegin, currentNode, textNodes, delimiterStack, definitionDictionary);
                    }
                    else
                    {
                        ProcessFullReferenceLink(text, ref i, ref textBegin, currentNode, textNodes, delimiterStack, definitionDictionary);
                    }
                    return;
                }
                // shortcut reference link
                ProcessShortcutReferenceLink(text, ref i, ref textBegin, currentNode, textNodes, delimiterStack, definitionDictionary);
                return;
            }
            else if (delimiterInfo.Type.HasFlag(DelimiterType.Image))
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
                    {
                        ProcessCollapsedReferenceImage(text, ref i, ref textBegin, currentNode, textNodes, delimiterStack, definitionDictionary);
                    }
                    else
                    {
                        ProcessFullReferenceImage(text, ref i, ref textBegin, currentNode, textNodes, delimiterStack, definitionDictionary);
                    }
                    return;
                }
                ProcessShortcutReferenceImage(text, ref i, ref textBegin, currentNode, textNodes, delimiterStack, definitionDictionary);
                return;
            }
            else
            {
                currentNode = currentNode.Previous;
            }
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

        int textBegin = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (ProcessLeafInline(text, ref i, ref textBegin, textNodes, parsers)) continue;

            // handle backslash escape
            if (text[i] is '\\' && i < text.Length - 1)
            {
                if (text[i + 1].IsAsciiPunctuation())
                {
                    i++;
                    continue;
                }
            }

            switch (text[i])
            {
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

        if (textBegin != text.Length)
            textNodes.AddLast((textBegin, new Text(text[textBegin..].ToString())));

        ProcessEmphasis(textNodes, delimiterStack, null);

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
        LinkedListNode<DelimiterNode>? currentPosition = stackBottom == null ? delimiterStack.First : stackBottom.Next;
        var openersBottomStar = stackBottom;
        var openersBottomUnderscore = stackBottom;

        bool multipleOfThree = false;

        while (currentPosition is not null)
        {
            if (currentPosition.Value.Type.HasFlag(DelimiterType.Closing) is false
                || currentPosition.Value.Type.HasFlag(DelimiterType.Star) is false
                    && currentPosition.Value.Type.HasFlag(DelimiterType.Underscore) is false)
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
                    || IsSameDelimiter(opener!.Value, currentPosition.Value) is false)
                {
                    opener = opener!.Previous;
                    continue;
                }


                if (opener.Value.Type.HasFlag(DelimiterType.Open | DelimiterType.Closing)
                    || currentPosition.Value.Type.HasFlag(DelimiterType.Open | DelimiterType.Closing))
                {
                    var lengthStart = ((Text)opener.Value.TextNode.Value.Inline).Content.Length;
                    var lengthEnd = ((Text)currentPosition.Value.TextNode.Value.Inline).Content.Length;
                    if ((lengthStart + lengthEnd) % 3 is 0 && (lengthStart % 3 is not 0 || lengthEnd % 3 is not 0))
                    {
                        opener = opener!.Previous;
                        multipleOfThree = true;
                        continue;
                    }
                }

                // found
                bool isStrong = false;
                if (opener.Value.Number >= 2 && currentPosition.Value.Number >= 2)
                {
                    isStrong = true;
                }

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

                for (var node = opener; node!.Next != currentPosition;)
                {
                    delimiterStack.Remove(node!.Next!);
                }

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

            if (opener == stackBottom || opener == openersBottom) // not found
            {
                if (!multipleOfThree) openersBottom = currentPosition!.Previous;
                multipleOfThree = false;

                var tmp = currentPosition!;
                currentPosition = currentPosition!.Next;

                if (tmp.Value.Type.HasFlag(DelimiterType.Open) is false)
                {
                    delimiterStack.Remove(tmp);
                }
            }
        }

        RemoveToEnd(delimiterStack, stackBottom == null ? delimiterStack.First : stackBottom.Next);
    }
}
