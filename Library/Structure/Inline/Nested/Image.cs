using CrackTC.SharpDown.Parsing;
using CrackTC.SharpDown.Structure;
using CrackTC.SharpDown.Structure.Inline;
using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Inline.Nested;

internal class Image : MarkdownInline
{
    public IEnumerable<MarkdownInline> Alternative { get; }
    public string Source { get; }
    public string Title { get; }

    public Image(IEnumerable<MarkdownInline> text, string destination, string? title = null)
    {
        Alternative = text;
        Source = destination;
        Title = title ?? string.Empty;
    }

    //public override XElement? ToHtml() => new("img",
    //                                          new XAttribute("alt", string.Concat(Alternative.Select(inline => inline.ToAST().FlattenText()))),
    //                                          new XAttribute("src", Source.Unescape()),
    //                                          string.IsNullOrEmpty(Title) ? null : new XAttribute("title", Title.Unescape()));

    public override string ToHtml(bool tight)
    {
        var src = Source.Unescape().HtmlEscape();
        var alt = string.Concat(Alternative.Select(inline => inline.ToAST().FlattenText())).HtmlEscape();
        if (string.IsNullOrEmpty(Title))
        {
            return $"<img src=\"{src}\" alt=\"{alt}\" />";
        }

        var title = Title.HtmlEscape();
        return $"<img src=\"{src}\" alt=\"{alt}\" title=\"{title}\" />";
    }

    public override XElement? ToAST() => new(MarkdownRoot.Namespace + "link",
                                             new XAttribute("destination", Source.Unescape()),
                                             new XAttribute("title", Title.Unescape()),
                                             Alternative.Select(inline => inline.ToAST()));

}
