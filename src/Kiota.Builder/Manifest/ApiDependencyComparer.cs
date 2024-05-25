﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        return x == null && y == null || x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }

    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] ApiDependency obj)
    {
        if (obj == null) return 0;

        int requestsHashCode = 0;
        if (CompareRequests && obj.Requests != null)
        {
            foreach (var request in obj.Requests)
            {
                requestsHashCode += requestInfoComparer.GetHashCode(request);
            }
        }

        return
            (string.IsNullOrEmpty(obj.ApiDescriptionUrl) ? 0 : obj.ApiDescriptionUrl.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 37 +
            (string.IsNullOrEmpty(obj.ApiDescriptionVersion) ? 0 : obj.ApiDescriptionVersion.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 31 +
            (obj.Extensions is not null && obj.Extensions.TryGetValue(GenerationConfiguration.KiotaHashManifestExtensionKey, out var jsonNode) && jsonNode is JsonValue jsonValue && jsonValue.GetValueKind() is JsonValueKind.String ? jsonValue.GetValue<string>().GetHashCode(StringComparison.OrdinalIgnoreCase) : 0) * 19 +
            requestsHashCode * 17;
    }
}
