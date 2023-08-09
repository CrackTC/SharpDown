using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Leaf;
using System.Text;

namespace CrackTC.SharpDown.Parsing.Block.Leaf;
internal class FencedCodeBlockParser : IMarkdownBlockParser
{
    private const string ValidChars = "`~";

    private static bool IsValidClosing(ReadOnlySpan<char> line, int columnNumber, int fenceCount, char? validChar)
    {
        var (count, _, _) = line.CountLeadingSpace(columnNumber, 4);
        if (count == 4) return false;

        var fence = line.TrimTabAndSpace();
        if (fence.Length < fenceCount) return false;
        foreach (var ch in fence) if (ch != validChar) return false;

        return true;
    }
    private static ReadOnlySpan<char> Skip(ReadOnlySpan<char> text, out string? infoString, out string? code)
    {
        infoString = string.Empty;
        code = string.Empty;

        var line = TextUtils.ReadLine(text, out var remaining, out var columnNumber, out _);

        var (indentation, index, _) = line.CountLeadingSpace(columnNumber, 4);
        if (indentation == 4) return text;

        char? validChar = null;
        int fenceCount = 0;
        for (int i = index; i < line.Length; i++)
        {
            if (validChar is null && ValidChars.Contains(line[i]) || line[i] == validChar)
            {
                validChar = line[i];
                fenceCount++;
            }
            else break;
        }

        if (fenceCount < 3) return text;

        infoString = line[(index + fenceCount)..].TrimTabAndSpace().ToString();
        if (validChar == '`' && infoString.Contains('`')) return text;

        var codeBuilder = new StringBuilder();
        while (!remaining.IsEmpty)
        {
            line = TextUtils.ReadLine(remaining, out remaining, out columnNumber, out _);
            if (IsValidClosing(line, columnNumber, fenceCount, validChar)) break;

            (_, index, var tabRemainingSpaces) = line.CountLeadingSpace(columnNumber, indentation);

            codeBuilder.Append(TextUtils.Space, tabRemainingSpaces)
                       .Append(line[index..])
                       .Append('\n');
        }

        code = codeBuilder.ToString();
        return remaining;
    }

    public bool TryReadAndParse(ref ReadOnlySpan<char> text, MarkdownBlock father, IEnumerable<IMarkdownBlockParser> blockParsers)
    {
        var remaining = Skip(text, out var infoString, out var code);
        if (remaining == text) return false;

        text = remaining;
        father.Children.Add(new FencedCodeBlock(infoString ?? string.Empty, code ?? string.Empty));

        return true;
    }
}
