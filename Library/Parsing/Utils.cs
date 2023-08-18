using CrackTC.SharpDown.Parsing.Inline.Nested;
using CrackTC.SharpDown.Structure;
using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Container;
using CrackTC.SharpDown.Structure.Block.Leaf;
using CrackTC.SharpDown.Structure.Inline;
using CrackTC.SharpDown.Structure.Inline.Leaf;

namespace CrackTC.SharpDown.Parsing;

internal static class Utils
{
    internal static bool IsNestedLastParagraph(ContainerBlock block)
    {
        while (true)
        {
            if (block.LastChild is not ContainerBlock container)
                return block.LastChild is Paragraph;
            block = container;
        }
    }

    internal static IEnumerable<LinkReferenceDefinition> GetLinkReferenceDefinitions(MarkdownBlock root)
    {
        return root switch
        {
            LinkReferenceDefinition definition => Enumerable.Repeat(definition, 1),
            ContainerBlock container => container.Children.SelectMany(child =>
                GetLinkReferenceDefinitions((MarkdownBlock)child)),
            MarkdownRoot rt => rt.Children.SelectMany(child => GetLinkReferenceDefinitions((MarkdownBlock)child)),
            _ => Enumerable.Empty<LinkReferenceDefinition>()
        };
    }

    internal static void CombineListItems(MarkdownBlock root)
    {
        var children = root.Children;
        foreach (var child in children)
            if (child is ContainerBlock block)
                CombineListItems(block);

        var newChildren = new List<MarkdownNode>();

        List list = null!;
        var listAssigned = false;
        var meetBlankLine = false;
        var markedAsLoose = false;
        foreach (var child in children)
            if (child is ListItem listItem)
            {
                if (!listAssigned)
                {
                    list = new List { Sign = listItem.Sign, Number = listItem.Number, IsOrdered = listItem.IsOrdered };
                }
                else if (!ListItem.IsSameType((ListItem)list.Children[0], listItem))
                {
                    list.IsLoose = markedAsLoose || IsListLoose(list);
                    newChildren.Add(list);

                    list = new List { Sign = listItem.Sign, Number = listItem.Number, IsOrdered = listItem.IsOrdered };
                }
                else if (meetBlankLine)
                {
                    markedAsLoose = true;
                }

                listAssigned = true;
                list.Children.Add(listItem);
            }
            else if (child is not BlankLine)
            {
                if (listAssigned)
                {
                    listAssigned = false;
                    list.IsLoose = markedAsLoose || IsListLoose(list);
                    markedAsLoose = false;
                    newChildren.Add(list);
                }

                meetBlankLine = false;
                newChildren.Add(child);
            }
            else
            {
                if (listAssigned) meetBlankLine = true;
                newChildren.Add(new BlankLine());
            }

        if (listAssigned)
        {
            list.IsLoose = markedAsLoose || IsListLoose(list);
            newChildren.Add(list);
        }

        root.Children = newChildren;
    }

    internal static IEnumerable<T> ElementsAfter<T>(LinkedListNode<T> node)
    {
        while (node.Next is not null)
        {
            node = node.Next;
            yield return node.Value;
        }
    }

    internal static void RemoveToEnd<T>(LinkedList<T> list, LinkedListNode<T>? node)
    {
        while (node is not null)
        {
            var tmp = node;
            node = node.Next;
            list.Remove(tmp);
        }
    }

    internal static void DeactivateLinks(LinkedListNode<DelimiterNode> node)
    {
        while (node.Previous is not null)
        {
            if (node.Previous.Value.Type.HasFlag(DelimiterType.Link))
                node.Previous.ValueRef.Type &= ~DelimiterType.Active;

            node = node.Previous;
        }
    }

    internal static void AppendTextRange(ReadOnlySpan<char> text,
        int begin,
        int end,
        LinkedList<(int StartIndex, MarkdownInline Inline)> textNodes)
    {
        if (begin != end) textNodes.AddLast((begin, new Text(text[begin..end].ToString())));
    }

    private static bool IsListLoose(MarkdownBlock list)
    {
        for (var i = 0; i < list.Children.Count; i++)
        {
            var listItem = (ListItem)list.Children[i];
            if (i != list.Children.Count - 1 && listItem.LastChild is BlankLine &&
                listItem.Children.Count != 1) return true;

            var skippedBlankLine = false;
            for (var j = 0; j < listItem.Children.Count; j++)
                if (listItem.Children[j] is not BlankLine) skippedBlankLine = true;
                else if (skippedBlankLine)
                    for (var k = j + 1; k < listItem.Children.Count; k++)
                        if (listItem.Children[k] is not BlankLine)
                            return true;
        }

        return false;
    }
}