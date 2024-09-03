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
    public ApiDependencyComparer(bool compareRequests = false)
    {
        CompareRequests = compareRequests;
    }
    private static readonly RequestInfoComparer requestInfoComparer = new();
    private readonly bool CompareRequests;

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

        const StringComparison sc = StringComparison.OrdinalIgnoreCase;
        if (!string.Equals(x.ApiDescriptionUrl, y.ApiDescriptionUrl, sc)) return false;
        if (!string.Equals(x.ApiDescriptionVersion, y.ApiDescriptionVersion, sc)) return false;

        string? xExtensions = GetDependencyExtensionsValue(x), yExtensions = GetDependencyExtensionsValue(y);
        // Assume requests are equal if we aren't comparing them.
        var requestsEqual = !CompareRequests || x.Requests.SequenceEqual(y.Requests, requestInfoComparer);
        return string.Equals(xExtensions, yExtensions, sc)
               && requestsEqual;
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] ApiDependency obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        var sc = StringComparer.OrdinalIgnoreCase;
        hash.Add(obj.ApiDescriptionUrl, sc);
        hash.Add(GetDependencyExtensionsValue(obj) ?? string.Empty, sc);
        if (!CompareRequests) return hash.ToHashCode();

        foreach (var request in obj.Requests)
        {
            hash.Add(request, requestInfoComparer);
        }
        return hash.ToHashCode();
    }
}
