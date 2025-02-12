using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.Lock;
/// <summary>
/// A comparer that compares two <see cref="IEnumerable{T}"/> of <see cref="string"/> by their content.
/// </summary>
public class StringIEnumerableDeepComparer : IEqualityComparer<IEnumerable<string>>
{
    private readonly StringComparer _stringComparer;
    private readonly bool _ordered;

    /// <summary>
    /// Creates a new instance of <see cref="StringIEnumerableDeepComparer"/>. This class performs equality comparison
    /// on elements of the <see cref="IEnumerable{T}"/> of <see cref="string"/>
    /// </summary>
    /// <param name="stringComparer">The string comparer to use when comparing 2 strings. Defaults to <see cref="StringComparer.OrdinalIgnoreCase"/></param>
    /// <param name="orderAgnosticComparison">Whether 2 collections with the same elements but different order should be considered equal.</param>
    public StringIEnumerableDeepComparer(StringComparer? stringComparer = null, bool orderAgnosticComparison = true)
    {
        _stringComparer = stringComparer ?? StringComparer.OrdinalIgnoreCase;
        _ordered = orderAgnosticComparison;
    }
    /// <inheritdoc/>
    public bool Equals(IEnumerable<string>? x, IEnumerable<string>? y)
    {
        if (x is null || y is null) return object.Equals(x, y);

        var x0 = _ordered ? x.Order(_stringComparer) : x;
        var y0 = _ordered ? y.Order(_stringComparer) : y;
        return x0.SequenceEqual(y0, _stringComparer);
    }
    /// <inheritdoc/>
    public int GetHashCode(IEnumerable<string> obj)
    {
        var hash = new HashCode();
        if (obj is null) return hash.ToHashCode();
        var items = _ordered ? obj.Order(_stringComparer) : obj;
        foreach (var item in items)
        {
            // hash code calculation is resistant to prefix collisions
            // i.e. "ab" + "cd" will not be the same as "abc" + "d"
            hash.Add(item, _stringComparer);
        }
        return hash.ToHashCode();
    }
}
