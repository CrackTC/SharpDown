using CrackTC.SharpDown.Parsing;
using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Inline.Nested
{
    internal class Link : MarkdownInline
    {
        private IEnumerable<MarkdownInline> Text { get; }
        private string Destination { get; }
        private string Title { get; }

        public Link(IEnumerable<MarkdownInline> text, string destination, string? title = null)
        {
            Text = text;
            Destination = destination;
            Title = title ?? string.Empty;
        }

        internal override string ToHtml(bool tight)
        {
            var content = string.Concat(Text.Select(inline => inline.ToHtml(tight)));
            var destination = Destination.AsSpan().HtmlUnescape().Unescape().HtmlEscape();
            if (string.IsNullOrEmpty(Title))
                return $"<a href=\"{destination}\">{content}</a>";
            
            var title = Title.AsSpan().HtmlUnescape().Unescape().HtmlEscape();
            return $"<a href=\"{destination}\" title=\"{title}\">{content}</a>";
        }

        public override XElement ToAst() => new(MarkdownRoot.Namespace + "link",
                                                new XAttribute("destination", Destination.Unescape()),
                                                new XAttribute("title", Title.Unescape()),
                                                Text.Select(inline => inline.ToAst()));
    }
}
