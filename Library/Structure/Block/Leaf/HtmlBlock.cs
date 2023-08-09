using System.Xml.Linq;
using CrackTC.SharpDown.Parsing.Inline.Leaf;

namespace CrackTC.SharpDown.Structure.Block.Leaf;

internal enum HtmlBlockType
{
    None,
    Text,
    Comment,
    ProcessingInstruction,
    Declaration,
    Cdata,
    Misc,
    Any
}

internal class HtmlBlock : LeafBlock
{
    private string Content { get; }

    public HtmlBlock(string content) => Content = content;

    //public override XElement? ToHtml() => new("raw", Content);
    public override string ToHtml(bool tight) => Content;

    public override XElement ToAst() => new(MarkdownRoot.Namespace + "html_block", Content);

    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
                                     IEnumerable<LinkReferenceDefinition> definitions)
    {
    }
}
