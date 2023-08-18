using System.Xml.Linq;
using CrackTC.SharpDown.Parsing.Inline.Leaf;
using CrackTC.SharpDown.Structure.Block.Leaf;

namespace CrackTC.SharpDown.Structure.Block.Container;

internal class List : ContainerBlock
{
    public int Number { get; internal init; }
    public bool IsOrdered { get; internal init; }
    public char Sign { get; internal init; }
    public bool IsLoose { get; internal set; }

    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
        IEnumerable<LinkReferenceDefinition> definitions)
    {
        Children.ForEach(child => ((MarkdownBlock)child).ParseInline(parsers, definitions));
    }

    public override XElement ToAst()
    {
        var content = Children.Select(child => child.ToAst());

        if (IsOrdered is false)
            return new XElement(MarkdownRoot.Namespace + "list",
                new XAttribute("type", "bullet"),
                new XAttribute("tight", !IsLoose),
                content);

        return new XElement(MarkdownRoot.Namespace + "list",
            new XAttribute("type", "ordered"),
            new XAttribute("start", Number),
            new XAttribute("tight", !IsLoose),
            new XAttribute("delimiter", Sign is '.' ? "period" : "paren"),
            content);
    }

    internal override string ToHtml(bool tight)
    {
        var content = string.Join('\n', Children.Select(child => child.ToHtml(!IsLoose)));

        if (IsOrdered is false) return $"<ul>\n{content}\n</ul>";

        return Number is 1 ? $"<ol>\n{content}\n</ol>" : $"<ol start=\"{Number}\">\n{content}\n</ol>";
    }
}