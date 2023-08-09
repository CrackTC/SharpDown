using CrackTC.SharpDown.Structure.Block.Container;
using CrackTC.SharpDown.Structure.Block.Leaf;

namespace CrackTC.SharpDown.Parsing;

internal static class Utils
{
    internal static bool IsNestedLastParagraph(ContainerBlock block)
    {
        while (true)
        {
            if (block.LastChild is not ContainerBlock container) return block.LastChild is Paragraph;
            block = container;
        }
    }
}
