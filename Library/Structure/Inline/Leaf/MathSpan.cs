using System.Xml.Linq;
using CrackTC.SharpDown.Parsing;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class MathSpan : MarkdownInline
{
    public MathSpan(string content)
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
        return new XElement(MarkdownRoot.Namespace + "math_span", Content);
    }
}