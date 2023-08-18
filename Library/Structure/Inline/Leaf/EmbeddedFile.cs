using System.Xml.Linq;
using CrackTC.SharpDown.Parsing;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class EmbeddedFile : MarkdownInline
{
    public EmbeddedFile(string source, string? attribute)
    {
        Source = source;
        Attribute = attribute;
    }

    private string Source { get; }
    private string? Attribute { get; }

    internal override string ToHtml(bool tight)
    {
        var source = Source.HtmlEscape();
        if (string.IsNullOrEmpty(Attribute)) return $"<div class=\"embedded-file\" data-src=\"{source}\"></div>";
        var attribute = Attribute.HtmlEscape();
        return $"<div class=\"embedded-file\" data-src=\"{source}\" data-attr=\"{attribute}\"></div>";
    }

    internal override XElement ToAst()
    {
        if (!string.IsNullOrEmpty(Attribute))
            return new XElement(MarkdownRoot.Namespace + "file",
                new XAttribute("source", Source),
                new XAttribute("attribute", Attribute));

        return new XElement(MarkdownRoot.Namespace + "file",
            new XAttribute("source", Source));
    }
}