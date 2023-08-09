using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure;

public abstract class MarkdownNode
{
    public abstract string ToHtml(bool tight);

    public abstract XElement? ToAST();
}
