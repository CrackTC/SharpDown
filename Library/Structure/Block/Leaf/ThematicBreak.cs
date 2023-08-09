using System.Xml.Linq;
using CrackTC.SharpDown.Parsing.Inline.Leaf;

namespace CrackTC.SharpDown.Structure.Block.Leaf;

internal class ThematicBreak : LeafBlock
{
    //public override XElement? ToHtml() => new("hr");
    public override string ToHtml(bool tight) => "<hr />";
    public override XElement ToAst() => new(MarkdownRoot.Namespace + "thematic_break");

    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
                                     IEnumerable<LinkReferenceDefinition> definitions)
    {
    }
}
