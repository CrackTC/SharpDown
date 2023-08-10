using CrackTC.SharpDown.Parsing;
using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class Autolink : MarkdownInline
{
    private string Destination { get; }
    private Text Label { get; }

    public Autolink(string destination, Text label)
    {
        Destination = destination;
        Label = label;
    }

    internal override string ToHtml(bool tight)
    {
        var destination = Destination.AsSpan().HtmlUnescape().HtmlEscape();
        var label = Label.Content.AsSpan().HtmlUnescape().HtmlEscape();
        return $"<a href=\"{destination}\">{label}</a>";
    }

    public override XElement ToAst() => new(MarkdownRoot.Namespace + "link",
                                            new XAttribute("destination", Destination),
                                            new XAttribute("title", string.Empty),
                                            Label.ToAst());
}