using System.Xml.Linq;
using CrackTC.SharpDown.Parsing;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class Math : MarkdownInline
{
    public Math(string content)
    {
        Content = content;
    }

    private string Content { get; }

    internal override string ToHtml(bool tight)
    {
        return Content.HtmlEscape();
    }

    public override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "math", Content);
    }
}