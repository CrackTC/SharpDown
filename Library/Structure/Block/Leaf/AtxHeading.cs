using System.Xml.Linq;
using CrackTC.SharpDown.Parsing;
using CrackTC.SharpDown.Parsing.Inline.Leaf;

namespace CrackTC.SharpDown.Structure.Block.Leaf;

internal class AtxHeading : LeafBlock
{
    public AtxHeading(int headingLevel, string content)
    {
        HeadingLevel = headingLevel;
        Content = content;
    }

    private int HeadingLevel { get; }
    private string Content { get; }

    internal override string ToHtml(bool tight)
    {
        return $"<h{HeadingLevel}>{string.Concat(Children.Select(child => child.ToHtml(tight)))}</h{HeadingLevel}>";
    }

    internal override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "heading",
            new XAttribute("level", HeadingLevel),
            Children.Select(child => child.ToAst()));
    }

    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
        IEnumerable<LinkReferenceDefinition> definitions)
    {
        MarkdownParser.ParseInline(Content, this, parsers, definitions);
    }
}