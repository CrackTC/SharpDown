using System.Xml.Linq;
using CrackTC.SharpDown.Parsing;
using CrackTC.SharpDown.Parsing.Inline.Leaf;

namespace CrackTC.SharpDown.Structure.Block.Leaf;

internal enum CellAlign
{
    None,
    Left,
    Right,
    Center
}

internal abstract class Cell : LeafBlock
{
    internal Cell(string content, CellAlign align)
    {
        Content = content;
        Align = align;
    }

    private string Content { get; }
    internal CellAlign Align { get; }

    internal static string GetAlignString(CellAlign align)
    {
        return align switch
        {
            CellAlign.None => "none",
            CellAlign.Left => "left",
            CellAlign.Right => "right",
            CellAlign.Center => "center",
            _ => throw new ArgumentOutOfRangeException(nameof(align))
        };
    }

    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
        IEnumerable<LinkReferenceDefinition> definitions)
    {
        MarkdownParser.ParseInline(Content, this, parsers, definitions);
    }
}

internal class HeaderCell : Cell
{
    public HeaderCell(string content, CellAlign align) : base(content, align)
    {
    }

    internal override string ToHtml(bool tight)
    {
        var content = string.Concat(Children.Select(child => child.ToHtml(tight)));
        if (Align == CellAlign.None) return $"<th>{content}</th>";
        var align = GetAlignString(Align);

        return $"<th align=\"{align}\">{content}</th>";
    }

    internal override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "header_cell",
            new XAttribute("align", GetAlignString(Align)),
            Children.Select(child => child.ToAst()));
    }
}

internal class DataCell : Cell
{
    public DataCell(string content, CellAlign align) : base(content, align)
    {
    }

    internal override string ToHtml(bool tight)
    {
        var content = string.Concat(Children.Select(child => child.ToHtml(tight)));
        if (Align == CellAlign.None) return $"<td>{content}</td>";
        var align = GetAlignString(Align);

        return $"<td align=\"{align}\">{content}</td>";
    }

    internal override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "data_cell",
            new XAttribute("align", GetAlignString(Align)),
            Children.Select(child => child.ToAst()));
    }
}

internal class TableRow : LeafBlock
{
    internal override string ToHtml(bool tight)
    {
        var content = string.Join('\n', Children.Select(child => child.ToHtml(tight)));
        return $"<tr>\n{content}\n</tr>";
    }

    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
        IEnumerable<LinkReferenceDefinition> definitions)
    {
        Children.ForEach(child => ((MarkdownBlock)child).ParseInline(parsers, definitions));
    }

    internal override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "table_row",
            Children.Select(child => child.ToAst()));
    }
}

internal class TableHead : LeafBlock
{
    internal TableHead(TableRow headRow)
    {
        HeadRow = headRow;
    }

    private TableRow HeadRow { get; }

    internal override string ToHtml(bool tight)
    {
        var content = HeadRow.ToHtml(tight);
        return $"<thead>\n{content}\n</thead>";
    }

    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
        IEnumerable<LinkReferenceDefinition> definitions)
    {
        HeadRow.ParseInline(parsers, definitions);
    }

    internal override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "table_head",
            HeadRow.ToAst());
    }
}

internal class TableBody : LeafBlock
{
    internal override string ToHtml(bool tight)
    {
        var content = string.Join('\n', Children.Select(child => child.ToHtml(tight)));
        return $"<tbody>\n{content}\n</tbody>";
    }

    internal override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "table_body",
            Children.Select(child => child.ToAst()));
    }

    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
        IEnumerable<LinkReferenceDefinition> definitions)
    {
        Children.ForEach(child => ((MarkdownBlock)child).ParseInline(parsers, definitions));
    }
}

internal class Table : LeafBlock
{
    internal Table(TableHead head, TableBody? body)
    {
        Head = head;
        Body = body;
    }

    private TableHead Head { get; }
    private TableBody? Body { get; }

    internal override void ParseInline(IEnumerable<IMarkdownLeafInlineParser> parsers,
        IEnumerable<LinkReferenceDefinition> definitions)
    {
        Head.ParseInline(parsers, definitions);
        Body?.ParseInline(parsers, definitions);
    }

    internal override string ToHtml(bool tight)
    {
        var head = Head.ToHtml(tight);
        if (Body == null) return $"<table>\n{head}\n</table>";
        var body = Body.ToHtml(tight);
        return $"<table>\n{head}\n{body}\n</table>";
    }

    internal override XElement ToAst()
    {
        return new XElement(MarkdownRoot.Namespace + "table",
            Head.ToAst(),
            Body?.ToAst());
    }
}