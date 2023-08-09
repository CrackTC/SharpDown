using System.Xml.Linq;
using CrackTC.SharpDown.Parsing.Inline.Leaf;
using CrackTC.SharpDown.Structure;

namespace CrackTC.SharpDown.Structure.Block.Leaf;

internal enum HtmlBlockType
{
    None,
    Text,
    Comment,
    ProcessingInstruction,
    Declaration,
    CDATA,
    Misc,
    Any
}

internal class HtmlBlock : LeafBlock
{
    public HtmlBlockType HtmlBlockType { get; }

    public string Content { get; }

    public HtmlBlock(string content, HtmlBlockType blockType)
    {
        Content = content;
        HtmlBlockType = blockType;
    }

    //public override XElement? ToHtml() => new("raw", Content);
    public override string ToHtml(bool tight) => Content;

    public override XElement? ToAST() => new(MarkdownRoot.Namespace + "html_block", Content);

    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
                                     IEnumerable<LinkReferenceDefinition> definitions)
    {
    }
}
