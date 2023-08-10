using CrackTC.SharpDown.Parsing;
using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class Text : MarkdownInline
{
    public string Content { get; }

    public Text(string content) => Content = content;

    internal override string ToHtml(bool tight) => Content.AsSpan().HtmlUnescape().Unescape().HtmlEscape();

    public override XElement ToAst() => new(MarkdownRoot.Namespace + "text", Content);
}
