using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi.ApiManifest;

namespace Kiota.Builder.Manifest;
public class RequestInfoComparer : IEqualityComparer<RequestInfo>
{
    public bool Equals(RequestInfo? x, RequestInfo? y)
    {
        if (x is null || y is null) return x?.Equals(y) == true;
        const StringComparison comparison = StringComparison.OrdinalIgnoreCase;
        return x.Method?.Equals(y.Method, comparison) == true && x.UriTemplate?.Equals(y.UriTemplate, comparison) == true;
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
