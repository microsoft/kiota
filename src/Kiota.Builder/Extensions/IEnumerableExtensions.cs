using System.Collections.Generic;

namespace Kiota.Builder.Extensions;

public static class IEnumerableExtensions
{
    /// <summary>
    /// Sums the values of an enumerable of integers, ignoring overflows.
    /// </summary>
    /// <param name="values">The values to sum.</param>
    /// <returns>The sum of the values.</returns>
    internal static int SumUnchecked(this IEnumerable<int> values)
    {
        if (values == null)
            return 0;

        int sum = 0;
        unchecked
        {
            foreach (var value in values)
            {
                sum += value;
            }
        }

        return sum;
    }
}
