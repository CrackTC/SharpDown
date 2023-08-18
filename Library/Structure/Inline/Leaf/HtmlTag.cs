using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class HtmlTag : MarkdownInline
{
    public HtmlTag(string content)
    {
        Content = content;
    }

    private string Content { get; }

    internal override string ToHtml(bool tight)
    {
        return Content;
    }

    public override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "html_inline", Content);
    }
}