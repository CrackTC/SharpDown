using System.Xml.Linq;
using CrackTC.SharpDown.Parsing.Inline.Leaf;
using CrackTC.SharpDown.Structure;
using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Leaf;

namespace CrackTC.SharpDown.Structure.Block.Container;

internal class BlockQuote : ContainerBlock
{
    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
                                     IEnumerable<LinkReferenceDefinition> definitions)
        => _children.ForEach(child => ((MarkdownBlock)child).ParseInline(parsers, definitions));

    public override XElement? ToAST() => new(MarkdownRoot.Namespace + "block_quote",
                                            _children.Select(child => child.ToAST()));

    //public override XElement? ToHtml() => new("blockquote",
    //                                         _children.Select(child => child.ToHtml()));

    public override string ToHtml(bool tight)
    {
        var content = string.Join('\n', _children.Where(child => child is not BlankLine and not LinkReferenceDefinition).Select(child => child.ToHtml(false)));
        return $"<blockquote>\n{(string.IsNullOrEmpty(content) ? string.Empty : content + '\n')}</blockquote>";
    }
}
