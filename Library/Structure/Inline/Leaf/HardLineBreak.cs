using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class HardLineBreak : MarkdownInline
{
    internal override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "linebreak");
    }

    internal override string ToHtml(bool tight)
    {
        return "<br />\n";
    }
}