using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class HtmlTag : MarkdownInline
{
    string Content { get; }

    public HtmlTag(string content)
    {
        Content = content;
    }

    //public override XElement? ToHtml() => new("raw", Content);
    public override string ToHtml(bool tight) => Content;

    public override XElement ToAst() => new(MarkdownRoot.Namespace + "html_inline", Content);
}