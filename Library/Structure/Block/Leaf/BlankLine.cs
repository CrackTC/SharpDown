using System.Xml.Linq;
using CrackTC.SharpDown.Parsing.Inline.Leaf;

namespace CrackTC.SharpDown.Structure.Block.Leaf;

internal class BlankLine : LeafBlock
{
    public string? Content { get; init; }
    //public override XElement? ToHtml() => null;
    public override string ToHtml(bool tight) => string.Empty;

    public override XElement? ToAST() => null;

    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
                                     IEnumerable<LinkReferenceDefinition> definitions)
    {
    }
}