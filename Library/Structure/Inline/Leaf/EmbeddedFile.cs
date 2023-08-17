using System.Xml.Linq;
using CrackTC.SharpDown.Parsing;

namespace CrackTC.SharpDown.Structure.Inline.Leaf;

internal class EmbeddedFile : MarkdownInline
{
    public EmbeddedFile(string source)
    {
        Source = source;
    }

    private string Source { get; }
    internal override string ToHtml(bool tight)
    {
        var source = Source.HtmlEscape();
        return $"<div class=\"embedded-file\" data-src=\"{source}\"/>";
    }

    public override XElement ToAst() =>
        new(MarkdownRoot.Namespace + "file",
            new XAttribute("source", Source));
}