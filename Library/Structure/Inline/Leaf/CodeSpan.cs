using CrackTC.SharpDown.Parsing;
using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class CodeSpan : MarkdownInline
{
    private string Content { get; }

    public CodeSpan(string content) => Content = content;

    internal override string ToHtml(bool tight) => $"<code>{Content.HtmlEscape()}</code>";

    public override XElement ToAst() => new(MarkdownRoot.Namespace + "code", Content);
}