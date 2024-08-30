using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DotNet.Globbing;

namespace Kiota.Builder.EqualityComparers;

internal class GlobComparer : IEqualityComparer<Glob>
{
    public bool Equals(Glob? x, Glob? y)
    {
        if (x is null || y is null) return object.Equals(x, y);
        return string.Equals(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public int GetHashCode([DisallowNull] Glob obj)
    {
        var hash = new HashCode();
        hash.Add(obj.ToString(), StringComparer.OrdinalIgnoreCase);
        return hash.ToHashCode();
    }
}
