using System.Xml.Linq;
using CrackTC.SharpDown.Structure;
using CrackTC.SharpDown.Structure.Inline;

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

    public override XElement? ToAST() => new(MarkdownRoot.Namespace + "html_inline", Content);
}