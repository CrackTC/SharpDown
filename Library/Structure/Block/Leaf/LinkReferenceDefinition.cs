using System.Xml.Linq;

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

    internal override string ToHtml(bool tight) => string.Empty;

    public override XElement? ToAst() => null;
}
