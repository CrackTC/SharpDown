using CrackTC.SharpDown.Structure.Inline;
using CrackTC.SharpDown.Structure.Inline.Leaf;
using System.Text;

namespace CrackTC.SharpDown.Parsing.Inline.Leaf;

internal class CodeSpanParser : IMarkdownLeafInlineParser
{
    private static string NormalizeContent(ReadOnlySpan<char> content)
    {
        var builder = new StringBuilder(content.Length);

        bool isAllSpace = true;
        while (content.IsEmpty is false)
        {
            if (content.TryRemoveLineEnding()) builder.Append(TextUtils.Space);
            else
            {
                builder.Append(content[0]);
                if (content[0].IsSpace() is false) isAllSpace = false;
                content = content[1..];
            }
        }

        if (builder[0].IsSpace() && builder[^1].IsSpace() && isAllSpace is false)
            return builder.ToString(1, builder.Length - 2);

        return builder.ToString();
    }

    public int TryReadAndParse(ReadOnlySpan<char> text, out MarkdownInline? inline)
    {
        if (text.StartsWith("`") is false)
        {
            inline = null;
            return 0;
        }

        int backtickCount = 1;
        for (int i = 1; i < text.Length; i++)
        {
            if (text[i] is '`') backtickCount++;
            else break;
        }

        int closingBacktickCount = 0;
        for (int i = backtickCount + 1; i < text.Length; i++)
        {
            if (text[i] == '`') closingBacktickCount++;
            else if (closingBacktickCount is not 0)
            {
                if (closingBacktickCount == backtickCount)
                {
                    var content = text[backtickCount..(i - backtickCount)];
                    inline = new CodeSpan(NormalizeContent(content));
                    return i;
                }
                else closingBacktickCount = 0;
            }
        }

        if (closingBacktickCount == backtickCount)
        {
            var content = text[backtickCount..(text.Length - backtickCount)];
            inline = new CodeSpan(NormalizeContent(content));
            return text.Length;
        }

        var builder = new StringBuilder(backtickCount);
        builder.Append('`', backtickCount);
        inline = new Text(builder.ToString());
        return backtickCount;
    }
}
