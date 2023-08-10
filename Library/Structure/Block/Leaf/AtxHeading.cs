using System.Xml.Linq;
using CrackTC.SharpDown.Parsing;
using CrackTC.SharpDown.Parsing.Inline.Leaf;

namespace CrackTC.SharpDown.Structure.Block.Leaf;
internal class AtxHeading : LeafBlock
{
    private int HeadingLevel { get; }
    private string Content { get; }

    public AtxHeading(int headingLevel, string content)
    {
        HeadingLevel = headingLevel;
        Content = content;
    }

    internal override string ToHtml(bool tight)
        => $"<h{HeadingLevel}>{string.Concat(Children.Select(child => child.ToHtml(tight)))}</h{HeadingLevel}>";

    public override XElement ToAst()
        => new(MarkdownRoot.Namespace + "heading",
               new XAttribute("level", HeadingLevel),
               Children.Select(child => child.ToAst()));

    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
                                     IEnumerable<LinkReferenceDefinition> definitions)
        => MarkdownParser.ParseInline(Content, this, parsers, definitions);

}