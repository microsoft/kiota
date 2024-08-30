using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Kiota.Builder.Lock;

/// <summary>
/// Compares two <see cref="KiotaLock"/> instances.
/// </summary>
public class KiotaLockComparer : IEqualityComparer<KiotaLock>
{
    private static readonly StringIEnumerableDeepComparer _stringIEnumerableDeepComparer = new();
    /// <inheritdoc/>
    public bool Equals(KiotaLock? x, KiotaLock? y)
    {
        if (x is null || y is null) return x?.Equals(y) == true;
        // Manual comparison to avoid false positives on hash collisions.
        return x.DisableSSLValidation.Equals(y.DisableSSLValidation) &&
               _stringIEnumerableDeepComparer.Equals(x.DisabledValidationRules, y.DisabledValidationRules) &&
               x.KiotaVersion.Equals(y.KiotaVersion, StringComparison.OrdinalIgnoreCase) &&
               x.LockFileVersion.Equals(y.LockFileVersion, StringComparison.OrdinalIgnoreCase) &&
               x.DescriptionLocation.Equals(y.DescriptionLocation, StringComparison.OrdinalIgnoreCase) &&
               x.DescriptionHash.Equals(y.DescriptionHash, StringComparison.OrdinalIgnoreCase) &&
               x.ClientClassName.Equals(y.ClientClassName, StringComparison.OrdinalIgnoreCase) &&
               x.ClientNamespaceName.Equals(y.ClientNamespaceName, StringComparison.OrdinalIgnoreCase) &&
               x.Language.Equals(y.Language, StringComparison.OrdinalIgnoreCase) &&
               x.ExcludeBackwardCompatible.Equals(y.ExcludeBackwardCompatible) &&
               x.UsesBackingStore.Equals(y.UsesBackingStore) &&
               x.IncludeAdditionalData.Equals(y.IncludeAdditionalData) &&
               _stringIEnumerableDeepComparer.Equals(x.Serializers, y.Serializers) &&
               _stringIEnumerableDeepComparer.Equals(x.Deserializers, y.Deserializers) &&
               _stringIEnumerableDeepComparer.Equals(x.StructuredMimeTypes, y.StructuredMimeTypes) &&
               _stringIEnumerableDeepComparer.Equals(x.IncludePatterns, y.IncludePatterns) &&
               _stringIEnumerableDeepComparer.Equals(x.ExcludePatterns, y.ExcludePatterns);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] KiotaLock obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        hash.Add(obj.DisableSSLValidation);
        hash.Add(obj.DisabledValidationRules, _stringIEnumerableDeepComparer);
        hash.Add(obj.KiotaVersion, StringComparer.Ordinal);
        hash.Add(obj.LockFileVersion, StringComparer.Ordinal);
        if (string.IsNullOrEmpty(obj.DescriptionLocation)) hash.Add(obj.DescriptionLocation, StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(obj.DescriptionHash)) hash.Add(obj.DescriptionHash, StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(obj.ClientClassName)) hash.Add(obj.ClientClassName, StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(obj.ClientNamespaceName)) hash.Add(obj.ClientNamespaceName, StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(obj.Language)) hash.Add(obj.Language, StringComparer.OrdinalIgnoreCase);
        hash.Add(obj.ExcludeBackwardCompatible);
        hash.Add(obj.UsesBackingStore);
        hash.Add(obj.IncludeAdditionalData);
        hash.Add(obj.Serializers, _stringIEnumerableDeepComparer);
        hash.Add(obj.Deserializers, _stringIEnumerableDeepComparer);
        hash.Add(obj.StructuredMimeTypes, _stringIEnumerableDeepComparer);
        hash.Add(obj.IncludePatterns, _stringIEnumerableDeepComparer);
        hash.Add(obj.ExcludePatterns, _stringIEnumerableDeepComparer);
        return hash.ToHashCode();
    }
}
