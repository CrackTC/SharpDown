using CrackTC.SharpDown.Structure.Block;
using CrackTC.SharpDown.Structure.Block.Leaf;
using System.Text;

namespace CrackTC.SharpDown.Parsing.Block.Leaf;

internal class IndentedCodeBlockParser : IMarkdownBlockParser
{
    private static ReadOnlySpan<char> Skip(ReadOnlySpan<char> text, out string content)
    {
        content = string.Empty;

        var line = TextUtils.ReadLine(text, out var remaining, out var columnNumber, out _);
        if (line.IsBlankLine()) return text;

        var codeBuilder = new StringBuilder();
        while (true)
        {
            var (count, index, tabRemainingSpaces) = line.CountLeadingSpace(columnNumber, 4);
            if (count < 4) break;
            codeBuilder.Append(TextUtils.Space, tabRemainingSpaces)
                       .Append(line[index..])
                       .Append('\n');

            text = remaining;
            if (remaining.IsEmpty) break;
            line = TextUtils.ReadLine(text, out remaining, out columnNumber, out _);
            if (line.IsBlankLine()) break;
        }

        content = codeBuilder.ToString();
        return text;
    }

    public bool TryReadAndParse(ref ReadOnlySpan<char> text,
                                MarkdownBlock father,
                                IEnumerable<IMarkdownBlockParser> blockParsers)
    {
        if (father.LastChild is Paragraph) return false;

        var remaining = Skip(text, out var code);

        if (remaining == text) return false;

        int i;
        for (i = father.Children.Count - 1; i >= 0; i--)
            if (father.Children[i] is not BlankLine) break;

        if (i >= 0 && father.Children[i] is IndentedCodeBlock block)
        {
            var builder = new StringBuilder(block.Code.Length + code.Length);

            builder.Append(block.Code);
            for (var j = i + 1; j < father.Children.Count; j++)
            {
                var blankLineSpan = ((BlankLine)father.Children[j]).Content.AsSpan();
                var (count, index, tabRemainingSpaces) = blankLineSpan.CountLeadingSpace(0, 4);
                if (count is 4) builder.Append(TextUtils.Space, tabRemainingSpaces);
                builder.Append(blankLineSpan[index..]);
                builder.Append('\n');
            }
            builder.Append(code);

            block.Code = builder.ToString();

            for (var j = i + 1; j < father.Children.Count; j++) father.Children.RemoveAt(father.Children.Count - 1);
        }
        else father.Children.Add(new IndentedCodeBlock(code));

        text = remaining;
        return true;
    }
}
