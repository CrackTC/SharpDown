using CrackTC.SharpDown.Parsing.Inline.Leaf;
using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Leaf;
using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure;

public class MarkdownRoot : MarkdownBlock
{
    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
                                     IEnumerable<LinkReferenceDefinition> definitions)
        => _children.ForEach(child => ((MarkdownBlock)child).ParseInline(parsers, definitions));

    internal static readonly XNamespace Namespace = XNamespace.Get("http://commonmark.org/xml/1.0");

    public override XElement ToAST() => new(Namespace + "document",
                                            _children.Where(child => child is not BlankLine)
                                                     .Select(child => child.ToAST()));

    //public override XElement ToHtml() => new("html",
    //                                         _children.Where(child => child is not BlankLine)
    //                                                  .Select(child => child.ToHtml()));

    public override string ToHtml(bool tight = false) => string.Join('\n', _children.Where(child => child is not BlankLine and not LinkReferenceDefinition)
                                                                                    .Select(child => child.ToHtml(tight)))
                                                               .Replace('\u0000', '\ufffd') + '\n';
}
