using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class HardLineBreak : MarkdownInline
{
    public override XElement ToAst() => new(MarkdownRoot.Namespace + "linebreak");

    internal override string ToHtml(bool tight) => "<br />\n";
}