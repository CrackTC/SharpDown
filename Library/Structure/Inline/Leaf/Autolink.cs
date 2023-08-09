using CrackTC.SharpDown.Parsing;
using CrackTC.SharpDown.Structure;
using CrackTC.SharpDown.Structure.Inline;
using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class Autolink : MarkdownInline
{
    public string Destination { get; }
    public Text Label { get; }

    public Autolink(string destination, Text label)
    {
        Destination = destination;
        Label = label;
    }

    //public override XElement? ToHtml() => new("a",
    //                                          new XAttribute("href", Destination),
    //                                          Label.ToHtml());
    public override string ToHtml(bool tight)
    {
        return $"<a href=\"{Destination.AsSpan().HtmlUnescape().HtmlEscape()}\">{Label.Content.AsSpan().HtmlUnescape().HtmlEscape()}</a>";
    }

    public override XElement? ToAST() => new(MarkdownRoot.Namespace + "link",
                                             new XAttribute("destination", Destination),
                                             new XAttribute("title", string.Empty),
                                             Label.ToAST());
}