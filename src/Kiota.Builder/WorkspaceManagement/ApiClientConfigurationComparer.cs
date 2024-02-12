using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Kiota.Builder.Lock;

namespace Kiota.Builder.WorkspaceManagement;
/// <summary>
/// Compares two <see cref="ApiClientConfiguration"/> instances.
/// </summary>
public class ApiClientConfigurationComparer : IEqualityComparer<ApiClientConfiguration>
{
    private static readonly StringIEnumerableDeepComparer _stringIEnumerableDeepComparer = new();
    /// <inheritdoc/>
    public bool Equals(ApiClientConfiguration? x, ApiClientConfiguration? y)
    {
        return x == null && y == null || x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] ApiClientConfiguration obj)
    {
        if (obj == null) return 0;
        return
            _stringIEnumerableDeepComparer.GetHashCode(obj.DisabledValidationRules?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 37 +
            (string.IsNullOrEmpty(obj.DescriptionLocation) ? 0 : obj.DescriptionLocation.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 31 +
            (string.IsNullOrEmpty(obj.OutputPath) ? 0 : obj.OutputPath.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 19 +
            (string.IsNullOrEmpty(obj.ClientNamespaceName) ? 0 : obj.ClientNamespaceName.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 23 +
            (string.IsNullOrEmpty(obj.Language) ? 0 : obj.Language.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 19 +
            obj.ExcludeBackwardCompatible.GetHashCode() * 17 +
            obj.UsesBackingStore.GetHashCode() * 13 +
            obj.IncludeAdditionalData.GetHashCode() * 7 +
            _stringIEnumerableDeepComparer.GetHashCode(obj.StructuredMimeTypes?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 5 +
            _stringIEnumerableDeepComparer.GetHashCode(obj.IncludePatterns?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 3 +
            _stringIEnumerableDeepComparer.GetHashCode(obj.ExcludePatterns?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 2;
    }
}
