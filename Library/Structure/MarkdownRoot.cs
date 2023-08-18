using System.Xml.Linq;
using CrackTC.SharpDown.Parsing.Inline.Leaf;
using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Leaf;

namespace CrackTC.SharpDown.Structure;

public class MarkdownRoot : MarkdownBlock
{
    internal static readonly XNamespace Namespace = XNamespace.Get("http://commonmark.org/xml/1.0");

    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
        IEnumerable<LinkReferenceDefinition> definitions)
    {
        Children.ForEach(child => ((MarkdownBlock)child).ParseInline(parsers, definitions));
    }

    public override XElement ToAst()
    {
        return new XElement(Namespace + "document",
            Children.Where(child => child is not BlankLine)
                .Select(child => child.ToAst()));
    }

    internal override string ToHtml(bool tight)
    {
        return string.Join('\n', Children.Where(child => child is not BlankLine and not LinkReferenceDefinition)
                .Select(child => child.ToHtml(tight)))
            .Replace('\u0000', '\ufffd') + '\n';
    }

    public string ToHtml()
    {
        return ToHtml(false);
    }
}