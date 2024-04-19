using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Kiota.Builder.Lock;

namespace Kiota.Builder.WorkspaceManagement;
public abstract class BaseApiConsumerConfigurationComparer<T> : IEqualityComparer<T> where T : BaseApiConsumerConfiguration
{
    private static readonly StringIEnumerableDeepComparer _stringIEnumerableDeepComparer = new();
    /// <inheritdoc/>
    public virtual bool Equals(T? x, T? y)
    {
        return x == null && y == null || x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }

    public virtual int GetHashCode([DisallowNull] T obj)
    {
        if (obj == null) return 0;
        return (string.IsNullOrEmpty(obj.DescriptionLocation) ? 0 : obj.DescriptionLocation.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 7 +
            (string.IsNullOrEmpty(obj.OutputPath) ? 0 : obj.OutputPath.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 5 +
            _stringIEnumerableDeepComparer.GetHashCode(obj.IncludePatterns?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 3 +
            _stringIEnumerableDeepComparer.GetHashCode(obj.ExcludePatterns?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 2;
    }
}
