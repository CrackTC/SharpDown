using CrackTC.SharpDown.Parsing.Inline.Leaf;
using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Leaf;
using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure;

public class MarkdownRoot : MarkdownBlock
{
    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
                                     IEnumerable<LinkReferenceDefinition> definitions)
        => Children.ForEach(child => ((MarkdownBlock)child).ParseInline(parsers, definitions));

    internal static readonly XNamespace Namespace = XNamespace.Get("http://commonmark.org/xml/1.0");

    public override XElement ToAst() => new(Namespace + "document",
                                            Children.Where(child => child is not BlankLine)
                                                    .Select(child => child.ToAst()));

    internal override string ToHtml(bool tight)
        => string.Join('\n', Children.Where(child => child is not BlankLine and not LinkReferenceDefinition)
                                     .Select(child => child.ToHtml(tight)))
                 .Replace('\u0000', '\ufffd') + '\n';

    public string ToHtml() => ToHtml(false);
}
