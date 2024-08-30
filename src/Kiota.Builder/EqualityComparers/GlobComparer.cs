using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DotNet.Globbing;

namespace Kiota.Builder.EqualityComparers;

internal class GlobComparer : IEqualityComparer<Glob>
{
    public bool Equals(Glob? x, Glob? y)
    {
        if (x is not null && y is not null)
        {
            return x.ToString().Equals(y.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        return x?.Equals(y) == true;
    }

    public int GetHashCode([DisallowNull] Glob obj)
    {
        var hash = new HashCode();
        hash.Add(obj.ToString(), StringComparer.OrdinalIgnoreCase);
        return hash.ToHashCode();
    }
}
