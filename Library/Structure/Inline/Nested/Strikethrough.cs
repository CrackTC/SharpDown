using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Inline.Nested;

internal class Strikethrough : MarkdownInline
{
    private readonly List<MarkdownInline> _children = new();

    public IList<MarkdownInline> Children => _children;

    internal override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "strikethrough",
            _children.Select(child => child.ToAst()));
    }

    internal override string ToHtml(bool tight)
    {
        var content = string.Concat(_children.Select(child => child.ToHtml(tight)));
        return $"<del>{content}</del>";
    }
}