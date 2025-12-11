using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Kiota.Builder.Lock;

namespace Kiota.Builder.WorkspaceManagement;

public abstract class BaseApiConsumerConfigurationComparer<T> : IEqualityComparer<T> where T : BaseApiConsumerConfiguration
{
    private readonly StringIEnumerableDeepComparer _stringIEnumerableDeepComparer;
    private readonly StringComparer _stringComparer;

    internal BaseApiConsumerConfigurationComparer(StringIEnumerableDeepComparer? stringIEnumerableDeepComparer = null,
        StringComparer? stringComparer = null)
    {
        _stringComparer = stringComparer ?? StringComparer.OrdinalIgnoreCase;
        _stringIEnumerableDeepComparer =
            stringIEnumerableDeepComparer ?? new StringIEnumerableDeepComparer(_stringComparer);
    }
    /// <inheritdoc/>
    public virtual bool Equals(T? x, T? y)
    {
        if (x is null || y is null) return object.Equals(x, y);
        return _stringComparer.Equals(x.DescriptionLocation, y.DescriptionLocation)
               && _stringComparer.Equals(x.OutputPath, y.OutputPath)
               && _stringIEnumerableDeepComparer.Equals(x.IncludePatterns, y.IncludePatterns);
    }

    public virtual int GetHashCode([DisallowNull] T obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        hash.Add(obj.DescriptionLocation, _stringComparer);
        hash.Add(obj.OutputPath, _stringComparer);
        hash.Add(obj.IncludePatterns, _stringIEnumerableDeepComparer);
        hash.Add(obj.ExcludePatterns, _stringIEnumerableDeepComparer);
        return hash.ToHashCode();
    }
}
