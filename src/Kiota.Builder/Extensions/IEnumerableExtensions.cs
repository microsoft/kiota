using System.Collections.Generic;
using System.Linq;

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
        unchecked
        {
            return values.Aggregate(0, static (acc, x) => acc + x);
        }
    }
}
