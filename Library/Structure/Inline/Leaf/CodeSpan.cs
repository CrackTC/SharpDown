using System.Xml.Linq;
using CrackTC.SharpDown.Parsing;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class CodeSpan : MarkdownInline
{
    public CodeSpan(string content)
    {
        Content = content;
    }

    private string Content { get; }

    internal override string ToHtml(bool tight)
    {
        return $"<code>{Content.HtmlEscape()}</code>";
    }

    internal override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "code", Content);
    }
}