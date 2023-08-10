using System.Xml.Linq;
using CrackTC.SharpDown.Parsing;

namespace CrackTC.SharpDown.Structure.Block.Leaf;

internal class IndentedCodeBlock : LeafBlock
{
    public string Code { get; internal set; }
    public IndentedCodeBlock(string code)
    {
        Code = code;
    }

    internal override string ToHtml(bool tight)
    {
        return $"<pre><code>{Code.HtmlEscape()}</code></pre>";
    }

    public override XElement ToAst() => new(MarkdownRoot.Namespace + "code_block", Code);
}
