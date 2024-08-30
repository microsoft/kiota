using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.Lock;
/// <summary>
/// A comparer that compares two <see cref="IEnumerable{T}"/> of <see cref="string"/> by their content.
/// </summary>
internal class StringIEnumerableDeepComparer : IEqualityComparer<IEnumerable<string>>
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
        if (x is not null && y is not null)
        {
            var x0 = _ordered ? x.Order() : x;
            var y0 = _ordered ? y.Order() : y;
            return x0.Order().SequenceEqual(y0, _stringComparer);
        }
        return x?.Equals(y) == true;
    }
    /// <inheritdoc/>
    public int GetHashCode(IEnumerable<string> obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        foreach (var item in obj)
        {
            hash.Add(item, _stringComparer);
            hash.Add(',');
        };
        return hash.ToHashCode();
    }
}
