using CrackTC.SharpDown.Parsing;
using CrackTC.SharpDown.Structure;
using CrackTC.SharpDown.Structure.Inline;
using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class CodeSpan : MarkdownInline
{
    public string Content { get; }

    public CodeSpan(string content)
    {
        Content = content;
    }

    //public override XElement? ToHtml() => new("code", Content);
    public override string ToHtml(bool tight) => $"<code>{Content.HtmlEscape()}</code>";

    public override XElement? ToAST() => new(MarkdownRoot.Namespace + "code", Content);
}