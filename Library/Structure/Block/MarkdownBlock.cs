using CrackTC.SharpDown.Parsing.Inline.Leaf;
using CrackTC.SharpDown.Structure;
using CrackTC.SharpDown.Structure.Block.Leaf;

namespace CrackTC.SharpDown.Structure.Block;

public abstract class MarkdownBlock : MarkdownNode
{
    protected List<MarkdownNode> _children = new();
    public List<MarkdownNode> Children
    {
        get => _children;
        set => _children = value;
    }

    internal MarkdownNode? LastChild
    {
        get => _children.Count is 0 ? null : _children[^1];
        set => _children[^1] = value!;
    }

    internal abstract void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
                                     IEnumerable<LinkReferenceDefinition> definitions);
}
