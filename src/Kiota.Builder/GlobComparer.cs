using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DotNet.Globbing;

namespace Kiota.Builder;

internal class GlobComparer : IEqualityComparer<Glob>
{
    public bool Equals(Glob? x, Glob? y)
    {
        return x == null && y == null || x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }

    public int GetHashCode([DisallowNull] Glob obj)
    {
        return obj.ToString().GetHashCode(StringComparison.OrdinalIgnoreCase);
    }
}
