using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi.ApiManifest;

namespace Kiota.Builder.Manifest;
public class RequestInfoComparer : IEqualityComparer<RequestInfo>
{
    public bool Equals(RequestInfo? x, RequestInfo? y)
    {
        if (x is null || y is null) return object.Equals(x, y);
        const StringComparison comparison = StringComparison.OrdinalIgnoreCase;
        return string.Equals(x.Method, y.Method, comparison) && string.Equals(x.UriTemplate, y.UriTemplate, comparison);
    }

    public int GetHashCode([DisallowNull] RequestInfo obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        var comparer = StringComparer.OrdinalIgnoreCase;
        hash.Add(obj.Method, comparer);
        hash.Add(obj.UriTemplate, comparer);
        return hash.ToHashCode();
    }
}
