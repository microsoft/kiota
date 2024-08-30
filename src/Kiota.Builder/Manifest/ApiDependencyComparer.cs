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
    /// <inheritdoc/>
    public bool Equals(ApiDependency? x, ApiDependency? y)
    {
        if (x is null || y is null) return x?.Equals(y) == true;
        string xExtensions = string.Empty, yExtensions = string.Empty;
        if (x.Extensions?.TryGetValue(GenerationConfiguration.KiotaHashManifestExtensionKey, out var n0) ==
            true && n0 is JsonValue valueX && valueX.GetValueKind() is JsonValueKind.String)
        {
            xExtensions = valueX.GetValue<string>();
        }
        if (y.Extensions?.TryGetValue(GenerationConfiguration.KiotaHashManifestExtensionKey, out var n1) ==
            true && n1 is JsonValue valueY && valueY.GetValueKind() is JsonValueKind.String)
        {
            yExtensions = valueY.GetValue<string>();
        }
        const StringComparison sc = StringComparison.OrdinalIgnoreCase;
        // Assume requests are equal if we aren't comparing them.
        var requestsEqual = !CompareRequests || x.Requests.SequenceEqual(y.Requests, requestInfoComparer);
        return x.ApiDescriptionUrl?.Equals(y.ApiDescriptionUrl, sc) == true
               && x.ApiDescriptionVersion?.Equals(y.ApiDescriptionVersion, sc) == true
               && xExtensions?.Equals(yExtensions, sc) == true
               && requestsEqual;
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] ApiDependency obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        var sc = StringComparer.OrdinalIgnoreCase;
        hash.Add(obj.ApiDescriptionUrl, sc);
        if (obj.Extensions?.TryGetValue(GenerationConfiguration.KiotaHashManifestExtensionKey, out var n0) ==
            true && n0 is JsonValue valueX)
        {
            hash.Add(valueX.GetValue<string>(), sc);
        }

        if (!CompareRequests) return hash.ToHashCode();

        foreach (var request in obj.Requests)
        {
            hash.Add(request, requestInfoComparer);
        }
        return hash.ToHashCode();
    }
}
