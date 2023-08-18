using System.Xml.Linq;
using CrackTC.SharpDown.Parsing;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class WikiLink : MarkdownInline
{
    public WikiLink(string? display, string destination)
    {
        Display = string.IsNullOrEmpty(display) ? destination : display;
        Destination = destination;
    }

    private string Display { get; }
    private string Destination { get; }

    internal override string ToHtml(bool tight)
    {
        var destination = Destination.HtmlEscape();
        var display = Display.HtmlEscape();
        return $"<a class=\"wiki-link\" href=\"{destination}\">{display}</a>";
    }

    public override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "wiki-link",
            new XAttribute("display", Display),
            new XAttribute("destination", Destination));
    }
}