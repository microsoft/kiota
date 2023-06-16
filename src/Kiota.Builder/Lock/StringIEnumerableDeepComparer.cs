using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.Lock;
/// <summary>
/// A comparer that compares two <see cref="IEnumerable{T}"/> of <see cref="string"/> by their content.
/// </summary>
internal class StringIEnumerableDeepComparer : IEqualityComparer<IEnumerable<string>>
{
    /// <inheritdoc/>
    public bool Equals(IEnumerable<string>? x, IEnumerable<string>? y)
    {
        return x == null && y == null || x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }
    /// <inheritdoc/>
    public int GetHashCode(IEnumerable<string> obj)
    {
        if (obj == null) return 0;
        return string.Join(",", obj.Order(StringComparer.OrdinalIgnoreCase)).GetHashCode(StringComparison.Ordinal);
    }
}
