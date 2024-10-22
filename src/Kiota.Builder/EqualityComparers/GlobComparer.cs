using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DotNet.Globbing;

namespace Kiota.Builder.EqualityComparers;

internal class GlobComparer(StringComparer? stringComparer = null) : IEqualityComparer<Glob>
{
    private readonly StringComparer _stringComparer = stringComparer ?? StringComparer.OrdinalIgnoreCase;

    public bool Equals(Glob? x, Glob? y)
    {
        if (x is null || y is null) return object.Equals(x, y);
        return _stringComparer.Equals(x.ToString(), y.ToString());
    }

    public int GetHashCode([DisallowNull] Glob obj)
    {
        return _stringComparer.GetHashCode(obj.ToString());
    }
}
