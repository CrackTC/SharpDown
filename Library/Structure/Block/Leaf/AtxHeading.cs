using System.Xml.Linq;
using CrackTC.SharpDown.Parsing;
using CrackTC.SharpDown.Parsing.Inline.Leaf;
using CrackTC.SharpDown.Structure;

namespace CrackTC.SharpDown.Structure.Block.Leaf;
internal class AtxHeading : LeafBlock
{
    public int HeadingLevel { get; }
    public string Content { get; }

    public AtxHeading(int headingLevel, string content)
    {
        HeadingLevel = headingLevel;
        Content = content;
    }

    //public override XElement? ToHtml() => new("h" + HeadingLevel, _children.Select(child => child.ToHtml()));
    public override string ToHtml(bool tight)
    {
        return $"<h{HeadingLevel}>{string.Concat(_children.Select(child => child.ToHtml(tight)))}</h{HeadingLevel}>";
    }

    public override XElement? ToAST() => new(MarkdownRoot.Namespace + "heading", new XAttribute("level", HeadingLevel), _children.Select(child => child.ToAST()));

    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
                                     IEnumerable<LinkReferenceDefinition> definitions)
        => MarkdownParser.ParseInline(Content, this, parsers, definitions);

}