using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class SoftLineBreak : MarkdownInline
{
    public override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "softbreak");
    }

    internal override string ToHtml(bool tight)
    {
        return "\n";
    }
}