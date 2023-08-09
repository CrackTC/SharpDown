using CrackTC.SharpDown.Structure.Block.Container;
using CrackTC.SharpDown.Structure.Block.Leaf;

namespace CrackTC.SharpDown.Parsing.Block.Container;

internal static class ListParser
{
    public static bool IsListLoose(List list)
    {
        for (int i = 0; i < list.Children.Count; i++)
        {
            var listItem = (ListItem)list.Children[i];
            if (i != list.Children.Count - 1 && listItem.LastChild is BlankLine && listItem.Children.Count != 1)
            {
                return true;
            }

            bool skippedBlankLine = false;
            for (int j = 0; j < listItem.Children.Count; j++)
            {
                if (listItem.Children[j] is not BlankLine)
                {
                    skippedBlankLine = true;
                }
                else if (skippedBlankLine)
                {
                    for (int k = j + 1; k < listItem.Children.Count; k++)
                        if (listItem.Children[k] is not BlankLine) return true;
                }
            }
        }
        return false;
    }

    public static bool IsSameType(List list1, List list2) => list1.IsOrdered == list2.IsOrdered && list1.Sign == list2.Sign;
}
