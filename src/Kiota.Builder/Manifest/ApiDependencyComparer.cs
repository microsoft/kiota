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
    private static readonly RequestInfoComparer requestInfoComparer = new();
    /// <inheritdoc/>
    public bool Equals(ApiDependency? x, ApiDependency? y)
    {
        return x == null && y == null || x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] ApiDependency obj)
    {
        if (obj == null) return 0;
        return
            (string.IsNullOrEmpty(obj.ApiDescriptionUrl) ? 0 : obj.ApiDescriptionUrl.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 37 +
            (string.IsNullOrEmpty(obj.ApiDescriptionVersion) ? 0 : obj.ApiDescriptionVersion.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 31 +
            (obj.Extensions is not null && obj.Extensions.TryGetValue(GenerationConfiguration.KiotaHashManifestExtensionKey, out var jsonNode) && jsonNode is JsonValue jsonValue && jsonValue.GetValueKind() is JsonValueKind.String ? jsonValue.GetValue<string>().GetHashCode(StringComparison.OrdinalIgnoreCase) : 0) * 19 +
            obj.Requests.Select(requestInfoComparer.GetHashCode).Sum() * 17;
    }
}
