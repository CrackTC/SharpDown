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
    public HtmlBlock(string content)
    {
        Content = content;
    }

    private string Content { get; }

    internal override string ToHtml(bool tight)
    {
        return Content;
    }

    internal override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "html_block", Content);
    }
}