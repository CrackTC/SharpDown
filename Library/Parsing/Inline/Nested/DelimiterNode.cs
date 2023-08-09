using CrackTC.SharpDown.Structure.Inline;

namespace CrackTC.SharpDown.Parsing.Inline.Nested;

[Flags]
internal enum DelimiterType
{
    None = 0,
    Open = 1 << 0,
    Closing = 1 << 1,
    Active = 1 << 2,
    Star = 1 << 3,
    Underscore = 1 << 4,
    Link = 1 << 5,
    Image = 1 << 6,
}

internal struct DelimiterNode
{
    public LinkedListNode<(int StartIndex, MarkdownInline Inline)> TextNode { get; init; }
    public DelimiterType Type { get; set; }
    public int Number { get; set; }
}
