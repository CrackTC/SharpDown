using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Block.Leaf;

internal class BlankLine : LeafBlock
{
    public string? Content { get; init; }

    internal override string ToHtml(bool tight) => string.Empty;

    public override XElement? ToAst() => null;
}