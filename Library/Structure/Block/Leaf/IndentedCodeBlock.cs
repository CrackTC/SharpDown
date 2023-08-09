using System.Xml.Linq;
using CrackTC.SharpDown.Parsing;
using CrackTC.SharpDown.Parsing.Inline.Leaf;

namespace CrackTC.SharpDown.Structure.Block.Leaf;

internal class IndentedCodeBlock : LeafBlock
{
    public string Code { get; internal set; }
    public IndentedCodeBlock(string code)
    {
        Code = code;
    }

    //public override XElement? ToHtml() => new("pre", new XElement("code", Code));
    public override string ToHtml(bool tight)
    {
        return $"<pre><code>{Code.HtmlEscape()}</code></pre>";
    }

    public override XElement ToAst() => new(MarkdownRoot.Namespace + "code_block", Code);

    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
                                     IEnumerable<LinkReferenceDefinition> definitions)
    {
    }
}
