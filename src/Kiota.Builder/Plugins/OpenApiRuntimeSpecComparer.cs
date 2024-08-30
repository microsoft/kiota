using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Plugins.Manifest;

namespace Kiota.Builder.Plugins;

public class OpenApiRuntimeSpecComparer : IEqualityComparer<OpenApiRuntimeSpec?>
{
    /// <inheritdoc/>
    public bool Equals(OpenApiRuntimeSpec? x, OpenApiRuntimeSpec? y)
    {
        if (x is null || y is null) return object.Equals(x, y);
        if (!string.Equals(x.Url, y.Url, StringComparison.OrdinalIgnoreCase)) return false;
        return string.Equals(x.ApiDescription, y.ApiDescription, StringComparison.OrdinalIgnoreCase) == true;
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] OpenApiRuntimeSpec obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        hash.Add(obj.Url, StringComparer.OrdinalIgnoreCase);
        hash.Add(obj.ApiDescription, StringComparer.OrdinalIgnoreCase);
        return hash.ToHashCode();
    }
}
