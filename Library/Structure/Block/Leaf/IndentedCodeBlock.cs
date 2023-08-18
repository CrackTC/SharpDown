using System.Xml.Linq;
using CrackTC.SharpDown.Parsing;

namespace CrackTC.SharpDown.Structure.Block.Leaf;

internal class IndentedCodeBlock : LeafBlock
{
    public IndentedCodeBlock(string code)
    {
        Code = code;
    }

    public string Code { get; internal set; }

    internal override string ToHtml(bool tight)
    {
        return $"<pre><code>{Code.HtmlEscape()}</code></pre>";
    }

    internal override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "code_block", Code);
    }
}