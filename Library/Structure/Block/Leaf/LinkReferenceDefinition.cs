using System.Xml.Linq;
using CrackTC.SharpDown.Parsing.Inline.Leaf;

namespace CrackTC.SharpDown.Structure.Block.Leaf;

internal class LinkReferenceDefinition : LeafBlock
{
    public string Label { get; }
    public string Destination { get; }
    public string Title { get; }

    public LinkReferenceDefinition(string label, string destination, string? title = null)
    {
        Label = label;
        Destination = destination;
        Title = title ?? string.Empty;
    }

    //public override XElement? ToHtml() => null;
    public override string ToHtml(bool tight) => string.Empty;

    public override XElement? ToAST() => null;

    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
                                     IEnumerable<LinkReferenceDefinition> definitions)
    {
    }
}
