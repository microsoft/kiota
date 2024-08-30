using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Plugins.Manifest;

namespace Kiota.Builder.Plugins;

internal class AuthComparer : IEqualityComparer<Auth?>
{
    /// <inheritdoc/>
    public bool Equals(Auth? x, Auth? y)
    {
        if (x is null || y is null) return object.Equals(x, y);
        // TODO: Should we compare the reference id as well?
        return x.Type == y.Type;
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] Auth obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        hash.Add(obj.Type, StringComparer.OrdinalIgnoreCase);
        return hash.ToHashCode();
    }
}
