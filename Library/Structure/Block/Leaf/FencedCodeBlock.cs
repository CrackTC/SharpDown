using System.Xml.Linq;
using CrackTC.SharpDown.Parsing;

namespace CrackTC.SharpDown.Structure.Block.Leaf;

internal class FencedCodeBlock : LeafBlock
{
    public FencedCodeBlock(string infoString, string code)
    {
        InfoString = infoString;
        Code = code;
    }

    private string InfoString { get; }
    private string Code { get; }

    internal override string ToHtml(bool tight)
    {
        var infoString = InfoString.AsSpan().HtmlUnescape().Unescape().HtmlEscape();
        var code = Code.HtmlEscape();
        return string.IsNullOrEmpty(InfoString)
            ? $"<pre><code>{code}</code></pre>"
            : $"<pre><code class=\"language-{infoString}\">{code}</code></pre>";
    }

    public override XElement ToAst()
    {
        if (string.IsNullOrEmpty(InfoString))
            return new XElement(MarkdownRoot.Namespace + "code_block", Code);

        return new XElement(MarkdownRoot.Namespace + "code_block",
            new XAttribute("info", InfoString),
            Code);
    }
}