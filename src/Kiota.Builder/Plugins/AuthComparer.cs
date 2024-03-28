using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Plugins.Manifest;

namespace Kiota.Builder.Plugins;

internal class AuthComparer : IEqualityComparer<Auth>
{
    /// <inheritdoc/>
    public bool Equals(Auth? x, Auth? y)
    {
        return x == null && y == null || x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] Auth obj)
    {
        if (obj == null) return 0;
        return string.IsNullOrEmpty(obj.Type) ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Type) * 3;
    }
}
