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
    private static readonly StringIEnumerableDeepComparer _stringIEnumerableDeepComparer = new();

    public override bool Equals(ApiClientConfiguration? x, ApiClientConfiguration? y)
    {
        if (x is null || y is null) return object.Equals(x, y);
        if (x.ExcludeBackwardCompatible != y.ExcludeBackwardCompatible) return false;
        if (x.UsesBackingStore != y.UsesBackingStore) return false;
        if (x.IncludeAdditionalData != y.IncludeAdditionalData) return false;
        if (!string.Equals(x.ClientNamespaceName, y.ClientNamespaceName, StringComparison.OrdinalIgnoreCase)) return false;
        if (!string.Equals(x.Language, y.Language, StringComparison.OrdinalIgnoreCase)) return false;

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
        var stringComparer = StringComparer.OrdinalIgnoreCase;
        hash.Add(obj.ClientNamespaceName, stringComparer);
        hash.Add(obj.Language, stringComparer);
        hash.Add(obj.ExcludeBackwardCompatible);
        hash.Add(obj.UsesBackingStore);
        hash.Add(obj.IncludeAdditionalData);
        hash.Add(obj.StructuredMimeTypes, _stringIEnumerableDeepComparer);
        hash.Add(base.GetHashCode(obj));
        return hash.ToHashCode();
    }
}
