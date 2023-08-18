using System.Xml.Linq;
using CrackTC.SharpDown.Parsing;
using CrackTC.SharpDown.Parsing.Inline.Leaf;

namespace CrackTC.SharpDown.Structure.Block.Leaf;

internal class Paragraph : LeafBlock
{
    public Paragraph(string content)
    {
        Content = content;
    }

    public string Content { get; }

    internal override string ToHtml(bool tight)
    {
        var content = string.Concat(Children.Select(child => child.ToHtml(tight)));
        return tight ? content : $"<p>{content}</p>";
    }

    public override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "paragraph", Children.Select(child => child.ToAst()));
    }

    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
        IEnumerable<LinkReferenceDefinition> definitions)
    {
        MarkdownParser.ParseInline(Content, this, parsers, definitions);
    }
}