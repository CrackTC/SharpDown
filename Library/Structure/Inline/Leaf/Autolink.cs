using System.Xml.Linq;
using CrackTC.SharpDown.Parsing;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class Autolink : MarkdownInline
{
    public Autolink(string destination, Text label)
    {
        Destination = destination;
        Label = label;
    }

    private string Destination { get; }
    private Text Label { get; }

    internal override string ToHtml(bool tight)
    {
        var destination = Destination.AsSpan().HtmlUnescape().HtmlEscape();
        var label = Label.Content.AsSpan().HtmlUnescape().HtmlEscape();
        return $"<a href=\"{destination}\">{label}</a>";
    }

    internal override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "link",
            new XAttribute("destination", Destination),
            new XAttribute("title", string.Empty),
            Label.ToAst());
    }
}