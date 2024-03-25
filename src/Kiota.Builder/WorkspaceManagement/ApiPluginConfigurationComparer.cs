using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Kiota.Builder.Lock;

namespace Kiota.Builder.WorkspaceManagement;
/// <summary>
/// Compares two <see cref="ApiPluginConfiguration"/> instances.
/// </summary>
public class ApiPluginConfigurationComparer : IEqualityComparer<ApiPluginConfiguration>
{
    private static readonly StringIEnumerableDeepComparer _stringIEnumerableDeepComparer = new();
    /// <inheritdoc/>
    public bool Equals(ApiPluginConfiguration? x, ApiPluginConfiguration? y)
    {
        return x == null && y == null || x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] ApiPluginConfiguration obj)
    {
        if (obj == null) return 0;
        return
            (string.IsNullOrEmpty(obj.DescriptionLocation) ? 0 : obj.DescriptionLocation.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 11 +
            (string.IsNullOrEmpty(obj.OutputPath) ? 0 : obj.OutputPath.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 7 +
            _stringIEnumerableDeepComparer.GetHashCode(obj.Types?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 5 +
            _stringIEnumerableDeepComparer.GetHashCode(obj.IncludePatterns?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 3 +
            _stringIEnumerableDeepComparer.GetHashCode(obj.ExcludePatterns?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 2;
    }
}
