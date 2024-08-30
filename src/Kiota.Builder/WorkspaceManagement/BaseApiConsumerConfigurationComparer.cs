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
        if (x is null || y is null) return x?.Equals(y) == true;
        const StringComparison comparison = StringComparison.OrdinalIgnoreCase;
        var comparer = StringComparer.OrdinalIgnoreCase;
        return x.DescriptionLocation.Equals(y.DescriptionLocation, comparison)
               && x.OutputPath.Equals(y.OutputPath, comparison)
               && x.IncludePatterns.SequenceEqual(y.IncludePatterns, comparer)
               && x.ExcludePatterns.SequenceEqual(y.ExcludePatterns, comparer);
    }

    public virtual int GetHashCode([DisallowNull] T obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        var comparer = StringComparer.OrdinalIgnoreCase;
        hash.Add(obj.DescriptionLocation, comparer);
        hash.Add(obj.OutputPath, comparer);
        hash.Add(obj.IncludePatterns, _stringIEnumerableDeepComparer);
        hash.Add(obj.ExcludePatterns, _stringIEnumerableDeepComparer);
        return hash.ToHashCode();
    }
}
