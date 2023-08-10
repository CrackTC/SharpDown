using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Block.Leaf;

internal class ThematicBreak : LeafBlock
{
    internal override string ToHtml(bool tight) => "<hr />";
    public override XElement ToAst() => new(MarkdownRoot.Namespace + "thematic_break");
}
