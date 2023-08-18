using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Inline.Nested;

internal class Emphasis : MarkdownInline
{
    private readonly List<MarkdownInline> _children = new();

    public Emphasis(bool isStrong)
    {
        IsStrong = isStrong;
    }

    public IList<MarkdownInline> Children => _children;
    private bool IsStrong { get; }

    public override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + (IsStrong ? "strong" : "emph"),
            _children.Select(child => child.ToAst()));
    }

    internal override string ToHtml(bool tight)
    {
        var content = string.Concat(_children.Select(child => child.ToHtml(tight)));
        return IsStrong ? $"<strong>{content}</strong>" : $"<em>{content}</em>";
    }
}