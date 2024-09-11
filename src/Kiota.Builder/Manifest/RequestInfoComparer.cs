using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi.ApiManifest;

namespace Kiota.Builder.Manifest;

/// <summary>
/// <see cref="IEqualityComparer{T}"/> for <see cref="RequestInfo"/> objects.
/// </summary>
/// <param name="stringComparer">
/// The string comparer to use when comparing string properties. Defaults to <see cref="StringComparer.OrdinalIgnoreCase"/>
/// </param>
public class RequestInfoComparer(StringComparer? stringComparer = null) : IEqualityComparer<RequestInfo>
{
    private readonly StringComparer _stringComparer = stringComparer ?? StringComparer.OrdinalIgnoreCase;

    public bool Equals(RequestInfo? x, RequestInfo? y)
    {
        if (x is null || y is null) return object.Equals(x, y);
        return _stringComparer.Equals(x.Method, y.Method) && _stringComparer.Equals(x.UriTemplate, y.UriTemplate);
    }

    public int GetHashCode([DisallowNull] RequestInfo obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        hash.Add(obj.Method, _stringComparer);
        hash.Add(obj.UriTemplate, _stringComparer);
        return hash.ToHashCode();
    }
}
