using CrackTC.SharpDown.Parsing.Inline.Leaf;
using CrackTC.SharpDown.Structure;
using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Leaf;
using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Block.Container;

internal class List : ContainerBlock
{
    public int Number { get; internal set; }
    public bool IsOrdered { get; internal set; }
    public char Sign { get; internal set; }
    public bool IsLoose { get; internal set; } = false;

    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
                                     IEnumerable<LinkReferenceDefinition> definitions)
        => _children.ForEach(child => ((MarkdownBlock)child).ParseInline(parsers, definitions));

    public override XElement? ToAST()
    {
        IEnumerable<XElement?> content = _children.Select(child => child.ToAST());

        if (IsOrdered is false)
        {
            return new(MarkdownRoot.Namespace + "list",
                       new XAttribute("type", "bullet"),
                       new XAttribute("tight", !IsLoose),
                       content);
        }

        return new(MarkdownRoot.Namespace + "list",
                   new XAttribute("type", "ordered"),
                   new XAttribute("start", Number),
                   new XAttribute("tight", !IsLoose),
                   new XAttribute("delimiter", Sign is '.' ? "period" : "paren"),
                   content);
    }

    //public override XElement? ToHtml()
    //{
    //    IEnumerable<XElement?> content;
    //    if (IsLoose)
    //    {
    //        content = _children.Select(child => child.ToHtml());
    //    }
    //    else
    //    {
    //        content = _children.Select(child => child is ListItem item ? item.ToHtmlTight() : child.ToHtml());
    //    }

    //    if (IsOrdered is false)
    //    {
    //        return new("ul", content);
    //    }

    //    if (Number is 1)
    //    {
    //        return new("ol", content);
    //    }

    //    return new("ol", new XAttribute("start", Number), content);
    //}

    public override string ToHtml(bool tight)
    {
        string content = string.Join('\n', _children.Select(child => child.ToHtml(!IsLoose)));

        if (IsOrdered is false)
        {
            return $"<ul>\n{content}\n</ul>";
        }

        if (Number is 1)
        {
            return $"<ol>\n{content}\n</ol>";
        }

        return $"<ol start=\"{Number}\">\n{content}\n</ol>";
    }
}
