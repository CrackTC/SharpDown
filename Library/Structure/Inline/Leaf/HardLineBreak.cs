using System.Xml.Linq;
using CrackTC.SharpDown.Structure;
using CrackTC.SharpDown.Structure.Inline;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class HardLineBreak : MarkdownInline
{
    public override XElement? ToAST() => new(MarkdownRoot.Namespace + "linebreak");

    //public override XElement? ToHtml() => new("br");
    public override string ToHtml(bool tight) => "<br />\n";
}