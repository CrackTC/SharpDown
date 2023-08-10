using System.Xml.Linq;

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

    internal override string ToHtml(bool tight) => Content;

    public override XElement ToAst() => new(MarkdownRoot.Namespace + "html_block", Content);
}
