using System.Xml.Linq;
using CrackTC.SharpDown.Parsing.Inline.Leaf;
using CrackTC.SharpDown.Structure.Block.Leaf;

namespace CrackTC.SharpDown.Structure.Block.Container;

internal class BlockQuote : ContainerBlock
{
    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
                                     IEnumerable<LinkReferenceDefinition> definitions)
        => Children.ForEach(child => ((MarkdownBlock)child).ParseInline(parsers, definitions));

    public override XElement ToAst() => new(MarkdownRoot.Namespace + "block_quote",
                                            Children.Select(child => child.ToAst()));

    internal override string ToHtml(bool tight)
    {
        var content = string.Join('\n', Children.Where(child => child is not BlankLine and not LinkReferenceDefinition)
                                                .Select(child => child.ToHtml(false)));
        return $"<blockquote>\n{(string.IsNullOrEmpty(content) ? string.Empty : content + '\n')}</blockquote>";
    }
}
