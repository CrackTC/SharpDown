using System.Diagnostics.CodeAnalysis;
using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Leaf;

namespace CrackTC.SharpDown.Parsing.Block.Leaf;

internal class TableParser : IMarkdownBlockParser
{
    public bool TryReadAndParse(ref ReadOnlySpan<char> text, MarkdownBlock father,
        IEnumerable<IMarkdownBlockParser> parsers)
    {
        if (!TryReadTable(ref text, parsers, out var table)) return false;
        father.Children.Add(table);
        return true;
    }

    private static void ReadCell(ref ReadOnlySpan<char> line, out string content)
    {
        if (!line.TryReadUtilUnescaped('|', out content))
        {
            content = line.ToString();
            line = ReadOnlySpan<char>.Empty;
        }

        content = content.Trim().Replace("\\|", "|");
    }

    private static void ReadPipe(ref ReadOnlySpan<char> line)
    {
        if (line.StartsWith("|")) line = line[1..];
    }

    private static bool TryReadTableRow(ref ReadOnlySpan<char> text, out List<string> strings)
    {
        strings = new List<string>();
        var line = TextUtils.ReadLine(text, out var remaining, out var columnNumber, out _);
        var (count, index, _) = line.CountLeadingSpace(columnNumber, 4);
        if (count == 4) return false;
        line = line[index..];

        ReadPipe(ref line);
        while (!line.IsEmpty)
        {
            ReadCell(ref line, out var content);
            strings.Add(content);
            ReadPipe(ref line);
        }

        text = remaining;
        return true;
    }

    private static bool IsValidTableDelimiter(ReadOnlySpan<char> delimiter)
    {
        if (delimiter.StartsWith(":")) delimiter = delimiter[1..];
        if (delimiter.EndsWith(":")) delimiter = delimiter[..^1];
        if (delimiter.IsEmpty) return false;
        foreach (var ch in delimiter)
            if (ch != '-')
                return false;

        return true;
    }

    private static bool TryReadTable(ref ReadOnlySpan<char> text, IEnumerable<IMarkdownBlockParser> parsers,
        [NotNullWhen(true)] out Table? table)
    {
        table = null;
        var tmp = text;

        if (!TryReadTableRow(ref tmp, out var headStrings)) return false;
        if (!TryReadTableRow(ref tmp, out var delimiterStrings)) return false;
        if (delimiterStrings.Count != headStrings.Count) return false;
        if (delimiterStrings.Any(s => !IsValidTableDelimiter(s))) return false;
        var aligns = delimiterStrings.Select(s =>
            (s.StartsWith(":"), s.EndsWith(":")) switch
            {
                (false, false) => CellAlign.None,
                (true, false) => CellAlign.Left,
                (false, true) => CellAlign.Right,
                (true, true) => CellAlign.Center
            }
        ).ToArray();

        var headerRow = new TableRow();
        headerRow.Children.AddRange(headStrings.Select((s, i) => new HeaderCell(s, aligns[i])));
        var head = new TableHead(headerRow);
        var body = new TableBody();

        text = tmp;
        while (!text.IsEmpty)
        {
            if (MarkdownParser.ParseBlock(ref tmp, new BlankLine(),
                    parsers.Where(p => p is not TableParser))) break;
            if (!TryReadTableRow(ref tmp, out var dataStrings)) break;
            dataStrings.Resize(headStrings.Count, string.Empty);
            var dataRow = new TableRow();
            dataRow.Children.AddRange(dataStrings.Select((s, i) => new DataCell(s, aligns[i])));
            body.Children.Add(dataRow);
            text = tmp;
        }

        table = body.Children.Count == 0 ? new Table(head, null) : new Table(head, body);
        return true;
    }
}