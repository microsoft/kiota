using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Kiota.Builder.Lock;

namespace Kiota.Builder.WorkspaceManagement;
/// <summary>
/// Compares two <see cref="ApiClientConfiguration"/> instances.
/// </summary>
public class ApiClientConfigurationComparer : BaseApiConsumerConfigurationComparer<ApiClientConfiguration>
{
    private readonly StringIEnumerableDeepComparer _stringIEnumerableDeepComparer;
    private readonly StringComparer _stringComparer;

    public ApiClientConfigurationComparer(StringIEnumerableDeepComparer? stringIEnumerableDeepComparer = null,
        StringComparer? stringComparer = null)
    {
        _stringComparer = stringComparer ?? StringComparer.OrdinalIgnoreCase;
        _stringIEnumerableDeepComparer =
            stringIEnumerableDeepComparer ?? new StringIEnumerableDeepComparer(_stringComparer);
    }

    public override bool Equals(ApiClientConfiguration? x, ApiClientConfiguration? y)
    {
        if (x is null || y is null) return object.Equals(x, y);
        if (x.ExcludeBackwardCompatible != y.ExcludeBackwardCompatible) return false;
        if (x.UsesBackingStore != y.UsesBackingStore) return false;
        if (x.IncludeAdditionalData != y.IncludeAdditionalData) return false;
        if (!_stringComparer.Equals(x.ClientNamespaceName, y.ClientNamespaceName)) return false;
        if (!_stringComparer.Equals(x.Language, y.Language)) return false;

        // slow deep comparison
        return _stringIEnumerableDeepComparer.Equals(x.DisabledValidationRules, y.DisabledValidationRules)
               && _stringIEnumerableDeepComparer.Equals(x.StructuredMimeTypes, y.StructuredMimeTypes);
    }

    /// <inheritdoc/>
    public override int GetHashCode([DisallowNull] ApiClientConfiguration obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        hash.Add(obj.DisabledValidationRules, _stringIEnumerableDeepComparer); // _stringIEnumerableDeepComparer orders
        hash.Add(obj.ClientNamespaceName, _stringComparer);
        hash.Add(obj.Language, _stringComparer);
        hash.Add(obj.ExcludeBackwardCompatible);
        hash.Add(obj.UsesBackingStore);
        hash.Add(obj.IncludeAdditionalData);
        hash.Add(obj.StructuredMimeTypes, _stringIEnumerableDeepComparer);
        hash.Add(base.GetHashCode(obj));
        return hash.ToHashCode();
    }
}
