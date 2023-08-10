using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class HtmlTag : MarkdownInline
{
    private string Content { get; }

    public HtmlTag(string content) => Content = content;

    internal override string ToHtml(bool tight) => Content;

    public override XElement ToAst() => new(MarkdownRoot.Namespace + "html_inline", Content);
}