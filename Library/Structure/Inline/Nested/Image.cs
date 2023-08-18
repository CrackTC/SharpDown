using System.Xml.Linq;
using CrackTC.SharpDown.Parsing;

namespace CrackTC.SharpDown.Structure.Inline.Nested;

internal class Image : MarkdownInline
{
    public Image(IEnumerable<MarkdownInline> text, string destination, string? title = null)
    {
        Alternative = text;
        Source = destination;
        Title = title ?? string.Empty;
    }

    private IEnumerable<MarkdownInline> Alternative { get; }
    private string Source { get; }
    private string Title { get; }

    internal override string ToHtml(bool tight)
    {
        var src = Source.Unescape().HtmlEscape();
        var alt = string.Concat(Alternative.Select(inline => inline.ToAst().FlattenText())).HtmlEscape();
        if (string.IsNullOrEmpty(Title)) return $"<img src=\"{src}\" alt=\"{alt}\" />";

        var title = Title.HtmlEscape();
        return $"<img src=\"{src}\" alt=\"{alt}\" title=\"{title}\" />";
    }

    public override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "link",
            new XAttribute("destination", Source.Unescape()),
            new XAttribute("title", Title.Unescape()),
            Alternative.Select(inline => inline.ToAst()));
    }
}