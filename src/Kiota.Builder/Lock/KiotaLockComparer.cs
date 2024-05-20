﻿using System;
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
        return x == null && y == null || x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] KiotaLock obj)
    {
        if (obj == null) return 0;
        return
            obj.DisableSSLValidation.GetHashCode() * 59 +
            _stringIEnumerableDeepComparer.GetHashCode(obj.DisabledValidationRules?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 53 +
            obj.KiotaVersion.GetHashCode(StringComparison.OrdinalIgnoreCase) * 47 +
            obj.LockFileVersion.GetHashCode(StringComparison.OrdinalIgnoreCase) * 43 +
            (string.IsNullOrEmpty(obj.DescriptionLocation) ? 0 : obj.DescriptionLocation.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 41 +
            (string.IsNullOrEmpty(obj.DescriptionHash) ? 0 : obj.DescriptionHash.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 37 +
            (string.IsNullOrEmpty(obj.ClientClassName) ? 0 : obj.ClientClassName.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 31 +
            (string.IsNullOrEmpty(obj.ClientNamespaceName) ? 0 : obj.ClientNamespaceName.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 29 +
            (string.IsNullOrEmpty(obj.Language) ? 0 : obj.Language.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 23 +
            obj.ExcludeBackwardCompatible.GetHashCode() * 19 +
            obj.UsesBackingStore.GetHashCode() * 17 +
            obj.IncludeAdditionalData.GetHashCode() * 13 +
            _stringIEnumerableDeepComparer.GetHashCode(obj.Serializers?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 11 +
            _stringIEnumerableDeepComparer.GetHashCode(obj.Deserializers?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 7 +
            _stringIEnumerableDeepComparer.GetHashCode(obj.StructuredMimeTypes?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 5 +
            _stringIEnumerableDeepComparer.GetHashCode(obj.IncludePatterns?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 3 +
            _stringIEnumerableDeepComparer.GetHashCode(obj.ExcludePatterns?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 2;
    }
}
