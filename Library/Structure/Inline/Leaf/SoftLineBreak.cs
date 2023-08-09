using System.Xml.Linq;
using CrackTC.SharpDown.Structure;
using CrackTC.SharpDown.Structure.Inline;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class SoftLineBreak : MarkdownInline
{
    public override XElement? ToAST() => new(MarkdownRoot.Namespace + "softbreak");

    //public override XElement? ToHtml() => new("raw", Environment.NewLine);
    public override string ToHtml(bool tight) => "\n";
}