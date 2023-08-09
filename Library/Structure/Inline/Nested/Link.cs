using CrackTC.SharpDown.Parsing;
using CrackTC.SharpDown.Structure;
using CrackTC.SharpDown.Structure.Inline;
using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Inline.Nested
{
    internal class Link : MarkdownInline
    {
        public IEnumerable<MarkdownInline> Text { get; }
        public string Destination { get; }
        public string Title { get; }

        public Link(IEnumerable<MarkdownInline> text, string destination, string? title = null)
        {
            Text = text;
            Destination = destination;
            Title = title ?? string.Empty;
        }

        //public override XElement? ToHtml() => new("a",
        //                                          new XAttribute("href", Destination.Unescape()),
        //                                          string.IsNullOrEmpty(Title) ? null : new XAttribute("title", Title.Unescape()),
        //                                          Text.Select(inline => inline.ToHtml()));
        public override string ToHtml(bool tight)
        {
            var content = string.Concat(Text.Select(inline => inline.ToHtml(tight)));
            if (string.IsNullOrEmpty(Title))
            {
                return $"<a href=\"{Destination.AsSpan().HtmlUnescape().Unescape().HtmlEscape()}\">{content}</a>";
            }
            return $"<a href=\"{Destination.AsSpan().HtmlUnescape().Unescape().HtmlEscape()}\" title=\"{Title.AsSpan().HtmlUnescape().Unescape().HtmlEscape()}\">{content}</a>";
        }

        public override XElement? ToAST() => new(MarkdownRoot.Namespace + "link",
                                                 new XAttribute("destination", Destination.Unescape()),
                                                 new XAttribute("title", Title.Unescape()),
                                                 Text.Select(inline => inline.ToAST()));
    }
}
