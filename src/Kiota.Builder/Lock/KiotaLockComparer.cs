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
        if (x is null || y is null) return object.Equals(x, y);
        // Manual comparison to avoid false positives on hash collisions.
        return x.DisableSSLValidation.Equals(y.DisableSSLValidation)
               && x.ExcludeBackwardCompatible.Equals(y.ExcludeBackwardCompatible)
               && x.UsesBackingStore.Equals(y.UsesBackingStore)
               && x.IncludeAdditionalData.Equals(y.IncludeAdditionalData)
               && x.KiotaVersion.Equals(y.KiotaVersion, StringComparison.OrdinalIgnoreCase)
               && x.LockFileVersion.Equals(y.LockFileVersion, StringComparison.OrdinalIgnoreCase)
               && x.DescriptionLocation.Equals(y.DescriptionLocation, StringComparison.OrdinalIgnoreCase)
               && x.DescriptionHash.Equals(y.DescriptionHash, StringComparison.OrdinalIgnoreCase)
               && x.ClientClassName.Equals(y.ClientClassName, StringComparison.OrdinalIgnoreCase)
               && x.ClientNamespaceName.Equals(y.ClientNamespaceName, StringComparison.OrdinalIgnoreCase)
               && x.Language.Equals(y.Language, StringComparison.OrdinalIgnoreCase)
               && _stringIEnumerableDeepComparer.Equals(x.DisabledValidationRules, y.DisabledValidationRules)
               && _stringIEnumerableDeepComparer.Equals(x.Serializers, y.Serializers)
               && _stringIEnumerableDeepComparer.Equals(x.Deserializers, y.Deserializers)
               && _stringIEnumerableDeepComparer.Equals(x.StructuredMimeTypes, y.StructuredMimeTypes)
               && _stringIEnumerableDeepComparer.Equals(x.IncludePatterns, y.IncludePatterns)
               && _stringIEnumerableDeepComparer.Equals(x.ExcludePatterns, y.ExcludePatterns);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] KiotaLock obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        var stringComparer = StringComparer.OrdinalIgnoreCase;
        hash.Add(obj.DisableSSLValidation);
        hash.Add(obj.DisabledValidationRules, _stringIEnumerableDeepComparer);
        hash.Add(obj.KiotaVersion, stringComparer);
        hash.Add(obj.LockFileVersion, stringComparer);
        hash.Add(obj.DescriptionLocation, stringComparer);
        hash.Add(obj.DescriptionHash, stringComparer);
        hash.Add(obj.ClientClassName, stringComparer);
        hash.Add(obj.ClientNamespaceName, stringComparer);
        hash.Add(obj.Language, stringComparer);
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
