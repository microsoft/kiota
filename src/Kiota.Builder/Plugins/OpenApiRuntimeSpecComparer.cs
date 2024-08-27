using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Plugins.Manifest;

namespace Kiota.Builder.Plugins;

public class OpenApiRuntimeSpecComparer : IEqualityComparer<OpenApiRuntimeSpec>
{
    /// <inheritdoc/>
    public bool Equals(OpenApiRuntimeSpec? x, OpenApiRuntimeSpec? y)
    {
        return x == null && y == null || x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] OpenApiRuntimeSpec obj)
    {
        if (obj == null) return 0;
        return (string.IsNullOrEmpty(obj.Url) ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Url) * 5) +
               (string.IsNullOrEmpty(obj.ApiDescription) ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ApiDescription) * 3);
    }
}
