using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Block.Leaf;

internal class BlankLine : LeafBlock
{
    public string? Content { get; init; }

    internal override string ToHtml(bool tight)
    {
        return string.Empty;
    }

    internal override XElement? ToAst()
    {
        return null;
    }
}