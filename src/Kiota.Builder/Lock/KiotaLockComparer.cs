using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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

        var disabledValidationRules = obj.DisabledValidationRules != null ? new List<string>(obj.DisabledValidationRules) : new List<string>();
        disabledValidationRules.Sort(StringComparer.OrdinalIgnoreCase);
        var serializers = obj.Serializers != null ? new List<string>(obj.Serializers) : new List<string>();
        serializers.Sort(StringComparer.OrdinalIgnoreCase);
        var deserializers = obj.Deserializers != null ? new List<string>(obj.Deserializers) : new List<string>();
        deserializers.Sort(StringComparer.OrdinalIgnoreCase);
        var structuredMimeTypes = obj.StructuredMimeTypes != null ? new List<string>(obj.StructuredMimeTypes) : new List<string>();
        structuredMimeTypes.Sort(StringComparer.OrdinalIgnoreCase);
        var includePatterns = obj.IncludePatterns != null ? new List<string>(obj.IncludePatterns) : new List<string>();
        includePatterns.Sort(StringComparer.OrdinalIgnoreCase);
        var excludePatterns = obj.ExcludePatterns != null ? new List<string>(obj.ExcludePatterns) : new List<string>();
        excludePatterns.Sort(StringComparer.OrdinalIgnoreCase);

        return
            obj.DisableSSLValidation.GetHashCode() * 59 +
            _stringIEnumerableDeepComparer.GetHashCode(disabledValidationRules) * 53 +
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
            _stringIEnumerableDeepComparer.GetHashCode(serializers) * 11 +
            _stringIEnumerableDeepComparer.GetHashCode(deserializers) * 7 +
            _stringIEnumerableDeepComparer.GetHashCode(structuredMimeTypes) * 5 +
            _stringIEnumerableDeepComparer.GetHashCode(includePatterns) * 3 +
            _stringIEnumerableDeepComparer.GetHashCode(excludePatterns) * 2;
    }
}
