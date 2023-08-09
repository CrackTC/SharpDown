using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Inline.Nested;

internal class Emphasis : MarkdownInline
{
    private readonly List<MarkdownInline> _children = new();
    public IList<MarkdownInline> Children => _children;
    private bool IsStrong { get; }

    public Emphasis(bool isStrong)
    {
        IsStrong = isStrong;
    }

    public override XElement ToAst() => new(MarkdownRoot.Namespace + (IsStrong ? "strong" : "emph"),
                                             _children.Select(child => child.ToAst()));

    //public override XElement? ToHtml() => new(IsStrong ? "strong" : "em",
    //                                          _children.Select(child => child.ToHtml()));
    public override string ToHtml(bool tight)
    {
        var content = string.Concat(_children.Select(child => child.ToHtml(tight)));
        if (IsStrong)
        {
            return $"<strong>{content}</strong>";
        }
        return $"<em>{content}</em>";
    }
}
