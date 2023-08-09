using CrackTC.SharpDown.Parsing;
using CrackTC.SharpDown.Parsing.Inline.Leaf;
using System.Xml.Linq;

namespace CrackTC.SharpDown.Structure.Block.Leaf;

internal class FencedCodeBlock : LeafBlock
{
    private string InfoString { get; }
    private string Code { get; }

    public FencedCodeBlock(string infoString, string code)
    {
        InfoString = infoString;
        Code = code;
    }

    //public override XElement? ToHtml()
    //{
    //    var content = _children.Select(child => child.ToHtml());

    //    if (string.IsNullOrEmpty(InfoString))
    //    {
    //        return new XElement("pre",
    //                            new XElement("code",
    //                                         content));
    //    }

    //    return new XElement("pre",
    //                        new XElement("code",
    //                                     new XAttribute("class", "language-" + InfoString),
    //                                     content));
    //}

    public override string ToHtml(bool tight)
    {
        return string.IsNullOrEmpty(InfoString) ? $"<pre><code>{Code.HtmlEscape()}</code></pre>"
                                                : $"<pre><code class=\"language-{InfoString.AsSpan().HtmlUnescape().Unescape().HtmlEscape()}\">{Code.HtmlEscape()}</code></pre>";
    }

    public override XElement ToAst()
    {
        if (string.IsNullOrEmpty(InfoString))
        {
            return new XElement(MarkdownRoot.Namespace + "code_block",
                                Code);
        }

        return new XElement(MarkdownRoot.Namespace + "code_block",
                            new XAttribute("info", InfoString),
                            Code);
    }

    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
                                     IEnumerable<LinkReferenceDefinition> definitions)
    {
    }
}
