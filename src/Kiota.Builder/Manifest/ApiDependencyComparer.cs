using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kiota.Builder.Configuration;
using Microsoft.OpenApi.ApiManifest;

namespace Kiota.Builder.Manifest;

public class ApiDependencyComparer : IEqualityComparer<ApiDependency>
{
    private readonly StringComparer _stringComparer;
    private readonly RequestInfoComparer _requestInfoComparer;
    private readonly bool _compareRequests;

    public ApiDependencyComparer(StringComparer? stringComparer = null, bool compareRequests = false)
    {
        _compareRequests = compareRequests;
        _stringComparer = stringComparer ?? StringComparer.OrdinalIgnoreCase;
        _requestInfoComparer = new RequestInfoComparer(_stringComparer);
    }

    private static string? GetDependencyExtensionsValue(ApiDependency dependency)
    {
        if (dependency.Extensions?.TryGetValue(GenerationConfiguration.KiotaHashManifestExtensionKey, out var n0) ==
            true && n0 is JsonValue valueX && valueX.GetValueKind() is JsonValueKind.String)
        {
            return valueX.GetValue<string>();
        }

        return null;
    }

    /// <inheritdoc/>
    public bool Equals(ApiDependency? x, ApiDependency? y)
    {
        if (x is null || y is null) return object.Equals(x, y);

        if (!_stringComparer.Equals(x.ApiDescriptionUrl, y.ApiDescriptionUrl)) return false;
        if (!_stringComparer.Equals(x.ApiDescriptionVersion, y.ApiDescriptionVersion)) return false;

        string? xExtensions = GetDependencyExtensionsValue(x), yExtensions = GetDependencyExtensionsValue(y);
        // Assume requests are equal if we aren't comparing them.
        var requestsEqual = !_compareRequests || GetOrderedRequests(x.Requests).SequenceEqual(GetOrderedRequests(y.Requests), _requestInfoComparer);
        return _stringComparer.Equals(xExtensions, yExtensions)
               && requestsEqual;
    }
    private static IOrderedEnumerable<RequestInfo> GetOrderedRequests(IList<RequestInfo> requests) =>
    requests.OrderBy(x => x.UriTemplate, StringComparer.Ordinal).ThenBy(x => x.Method, StringComparer.Ordinal);
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] ApiDependency obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        hash.Add(obj.ApiDescriptionUrl, _stringComparer);
        hash.Add(GetDependencyExtensionsValue(obj) ?? string.Empty, _stringComparer);
        if (!_compareRequests) return hash.ToHashCode();

        foreach (var request in GetOrderedRequests(obj.Requests))
        {
            hash.Add(request, _requestInfoComparer);
        }
        return hash.ToHashCode();
    }
}
