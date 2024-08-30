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
        if (x is null || y is null) return x?.Equals(y) == true;
        if (!x.Url?.Equals(y.Url, StringComparison.OrdinalIgnoreCase) == false) return false;
        return x.ApiDescription?.Equals(y.ApiDescription, StringComparison.OrdinalIgnoreCase) == true;
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] OpenApiRuntimeSpec obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        if (obj.Url is not null) hash.Add(obj.Url, StringComparer.OrdinalIgnoreCase);
        if (obj.ApiDescription is not null) hash.Add(obj.ApiDescription, StringComparer.OrdinalIgnoreCase);
        return hash.ToHashCode();
    }
}
