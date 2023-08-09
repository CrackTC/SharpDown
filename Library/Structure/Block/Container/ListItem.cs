using CrackTC.SharpDown.Parsing.Inline.Leaf;
using CrackTC.SharpDown.Structure.Block.Leaf;
using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Block.Container;

internal class ListItem : ContainerBlock
{
    public bool IsOrdered { get; }
    public int Number { get; }
    public char Sign { get; }

    public ListItem(char sign, bool isOrdered, int number)
    {
        Sign = sign;
        IsOrdered = isOrdered;
        Number = number;
    }

    public static bool IsSameType(ListItem a, ListItem b) => a.IsOrdered == b.IsOrdered && a.Sign == b.Sign;

    //public XElement ToHtmlTight()
    //{
    //    var tightContent = ToHtml()!.Elements().Select<XElement, object>(child => child.Name.LocalName is "p" ? child.Nodes() : child);
    //    return new("li", tightContent);
    //}

    //public override XElement? ToHtml() => new("li", _children.Select(_children => _children.ToHtml()));
    public override string ToHtml(bool tight)
    {
        var content = string.Join('\n', Children.Where(child => child is not BlankLine and not LinkReferenceDefinition)
                                                .Select(child => child.ToHtml(tight)));
        if (string.IsNullOrEmpty(content)) return "<li></li>";

        bool beginLineEnding = true, endLineEnding = true;
        if (tight && Children.Any(child => child is Paragraph))
        {
            if (Children.First(child => child is not BlankLine) is Paragraph) beginLineEnding = false;
            if (Children.Last(child => child is not BlankLine) is Paragraph) endLineEnding = false;
        }
        return $"<li>{(beginLineEnding ? "\n" : "")}{content}{(endLineEnding ? "\n" : "")}</li>";
    }

    public override XElement ToAst() => new(MarkdownRoot.Namespace + "item", Children.Select(child => child.ToAst()));

    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
                                     IEnumerable<LinkReferenceDefinition> definitions)
        => Children.ForEach(child => ((MarkdownBlock)child).ParseInline(parsers, definitions));
}