using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi.ApiManifest;

namespace Kiota.Builder.Manifest;
public class RequestInfoComparer : IEqualityComparer<RequestInfo>
{
    public bool Equals(RequestInfo? x, RequestInfo? y)
    {
        return x == null && y == null || x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }

    public int GetHashCode([DisallowNull] RequestInfo obj)
    {
        if (obj == null) return 0;
        return (string.IsNullOrEmpty(obj.Method) ? 0 : obj.Method.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 7 +
            (string.IsNullOrEmpty(obj.UriTemplate) ? 0 : obj.UriTemplate.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 3;
    }
}
