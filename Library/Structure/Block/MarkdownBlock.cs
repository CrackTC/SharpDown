using CrackTC.SharpDown.Parsing.Inline.Leaf;
using CrackTC.SharpDown.Structure.Block.Leaf;

namespace CrackTC.SharpDown.Structure.Block;

public abstract class MarkdownBlock : MarkdownNode
{
    public List<MarkdownNode> Children { get; set; } = new();

    internal MarkdownNode? LastChild
    {
        get => Children.Count is 0 ? null : Children[^1];
        set => Children[^1] = value!;
    }

    internal virtual void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
        IEnumerable<LinkReferenceDefinition> definitions)
    {
    }
}
