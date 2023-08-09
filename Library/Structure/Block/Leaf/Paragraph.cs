using CrackTC.SharpDown.Parsing;
using CrackTC.SharpDown.Parsing.Inline.Leaf;
using CrackTC.SharpDown.Structure;
using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Block.Leaf
{
    internal class Paragraph : LeafBlock
    {
        public string Content { get; }

        public Paragraph(string content)
        {
            Content = content;
        }

        //public override XElement? ToHtml() => new("p", _children.Select(child => child.ToHtml()));
        public override string ToHtml(bool tight)
        {
            if (tight)
            {
                return string.Concat(_children.Select(child => child.ToHtml(true)));
            }
            else
            {
                return $"<p>{string.Concat(_children.Select(child => child.ToHtml(false)))}</p>";
            }
        }

        public override XElement? ToAST() => new(MarkdownRoot.Namespace + "paragraph", _children.Select(child => child.ToAST()));

        internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
                                         IEnumerable<LinkReferenceDefinition> definitions)
            => MarkdownParser.ParseInline(Content, this, parsers, definitions);
    }
}