using CrackTC.SharpDown.Structure.Block.Container;
using CrackTC.SharpDown.Structure.Block.Leaf;

namespace CrackTC.SharpDown.Parsing;

internal static class Utils
{

    internal static bool IsNestedLastParagraph(ContainerBlock block)
        => block.LastChild is ContainerBlock container ? IsNestedLastParagraph(container) : block.LastChild is Paragraph;
}
