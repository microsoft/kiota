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
    private readonly StringIEnumerableDeepComparer _stringIEnumerableDeepComparer;
    private readonly StringComparer _stringComparer;

    public KiotaLockComparer(StringComparer? stringComparer = null)
    {
        _stringComparer = stringComparer ?? StringComparer.OrdinalIgnoreCase;
        _stringIEnumerableDeepComparer = new StringIEnumerableDeepComparer(_stringComparer);
    }

    /// <inheritdoc/>
    public bool Equals(KiotaLock? x, KiotaLock? y)
    {
        if (x is null || y is null) return object.Equals(x, y);
        // Manual comparison to avoid false positives on hash collisions.
        return x.DisableSSLValidation == y.DisableSSLValidation
               && x.ExcludeBackwardCompatible == y.ExcludeBackwardCompatible
               && x.UsesBackingStore == y.UsesBackingStore
               && x.IncludeAdditionalData == y.IncludeAdditionalData
               && _stringComparer.Equals(x.KiotaVersion, y.KiotaVersion)
               && _stringComparer.Equals(x.LockFileVersion, y.LockFileVersion)
               && _stringComparer.Equals(x.DescriptionLocation, y.DescriptionLocation)
               && _stringComparer.Equals(x.DescriptionHash, y.DescriptionHash)
               && _stringComparer.Equals(x.ClientClassName, y.ClientClassName)
               && _stringComparer.Equals(x.ClientNamespaceName, y.ClientNamespaceName)
               && _stringComparer.Equals(x.Language, y.Language)
               && _stringComparer.Equals(x.TypeAccessModifier, y.TypeAccessModifier)
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
        hash.Add(obj.DisableSSLValidation);
        hash.Add(obj.DisabledValidationRules, _stringIEnumerableDeepComparer);
        hash.Add(obj.KiotaVersion, _stringComparer);
        hash.Add(obj.LockFileVersion, _stringComparer);
        hash.Add(obj.DescriptionLocation, _stringComparer);
        hash.Add(obj.DescriptionHash, _stringComparer);
        hash.Add(obj.ClientClassName, _stringComparer);
        hash.Add(obj.ClientNamespaceName, _stringComparer);
        hash.Add(obj.Language, _stringComparer);
        hash.Add(obj.TypeAccessModifier, _stringComparer);
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
