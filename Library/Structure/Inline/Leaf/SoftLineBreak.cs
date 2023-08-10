using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class SoftLineBreak : MarkdownInline
{
    public override XElement ToAst() => new(MarkdownRoot.Namespace + "softbreak");

    internal override string ToHtml(bool tight) => "\n";
}