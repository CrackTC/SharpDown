using System.Xml.Linq;
using CrackTC.SharpDown.Parsing;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class Text : MarkdownInline
{
    public Text(string content)
    {
        Content = content;
    }

    public string Content { get; }

    internal override string ToHtml(bool tight)
    {
        return Content.AsSpan().HtmlUnescape().Unescape().HtmlEscape();
    }

    public override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "text", Content);
    }
}