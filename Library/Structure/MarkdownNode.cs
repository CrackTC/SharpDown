using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure;

public abstract class MarkdownNode
{
    internal abstract string ToHtml(bool tight);

    public abstract XElement? ToAst();
}