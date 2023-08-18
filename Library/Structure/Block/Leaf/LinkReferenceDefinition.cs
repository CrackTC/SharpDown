using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Block.Leaf;

internal class LinkReferenceDefinition : LeafBlock
{
    public LinkReferenceDefinition(string label, string destination, string? title = null)
    {
        Label = label;
        Destination = destination;
        Title = title ?? string.Empty;
    }

    public string Label { get; }
    public string Destination { get; }
    public string Title { get; }

    internal override string ToHtml(bool tight)
    {
        return string.Empty;
    }

    internal override XElement? ToAst()
    {
        return null;
    }
}