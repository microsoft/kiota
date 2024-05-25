using System;
using System.Collections.Generic;

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

        var list = new List<string>(obj);
        list.Sort(StringComparer.OrdinalIgnoreCase);
        var concatValue = string.Join(",", list);

        return string.IsNullOrEmpty(concatValue) ? 0 : concatValue.GetHashCode(StringComparison.Ordinal);
    }
}
