using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Plugins.Manifest;

namespace Kiota.Builder.Plugins;

public class OpenApiRuntimeSpecComparer(StringComparer? stringComparer = null) : IEqualityComparer<OpenApiRuntimeSpec?>
{
    private readonly StringComparer _stringComparer = stringComparer ?? StringComparer.OrdinalIgnoreCase;

    /// <inheritdoc/>
    public bool Equals(OpenApiRuntimeSpec? x, OpenApiRuntimeSpec? y)
    {
        if (x is null || y is null) return object.Equals(x, y);
        return _stringComparer.Equals(x.Url, y.Url) && _stringComparer.Equals(x.ApiDescription, y.ApiDescription);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] OpenApiRuntimeSpec obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        hash.Add(obj.Url, _stringComparer);
        hash.Add(obj.ApiDescription, _stringComparer);
        return hash.ToHashCode();
    }
}
