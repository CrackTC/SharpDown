using System.Xml.Linq;
using CrackTC.SharpDown.Parsing;

namespace CrackTC.SharpDown.Structure.Block.Leaf;

internal class MathBlock : LeafBlock
{
    public MathBlock(string content)
    {
        Content = content;
    }

    private string Content { get; }

    internal override string ToHtml(bool tight)
    {
        return Content.HtmlEscape();
    }

    internal override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "math_block", Content);
    }
}