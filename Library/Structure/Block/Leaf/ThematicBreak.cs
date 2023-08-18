using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Block.Leaf;

internal class ThematicBreak : LeafBlock
{
    internal override string ToHtml(bool tight)
    {
        return "<hr />";
    }

    internal override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "thematic_break");
    }
}