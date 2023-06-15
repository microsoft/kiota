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
        return x == null && y == null || x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] KiotaLock obj)
    {
        if (obj == null) return 0;
        return
            _stringIEnumerableDeepComparer.GetHashCode(obj.DisabledValidationRules?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 47 +
            GetVersionHashCode(obj.KiotaVersion) * 43 +
            GetVersionHashCode(obj.LockFileVersion) * 41 +
            (string.IsNullOrEmpty(obj.DescriptionLocation) ? 0 : obj.DescriptionLocation.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 37 +
            (string.IsNullOrEmpty(obj.DescriptionHash) ? 0 : obj.DescriptionHash.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 31 +
            (string.IsNullOrEmpty(obj.ClientClassName) ? 0 : obj.ClientClassName.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 29 +
            (string.IsNullOrEmpty(obj.ClientNamespaceName) ? 0 : obj.ClientNamespaceName.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 23 +
            (string.IsNullOrEmpty(obj.Language) ? 0 : obj.Language.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 19 +
            obj.UsesBackingStore.GetHashCode() * 17 +
            obj.IncludeAdditionalData.GetHashCode() * 13 +
            _stringIEnumerableDeepComparer.GetHashCode(obj.Serializers?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 11 +
            _stringIEnumerableDeepComparer.GetHashCode(obj.Deserializers?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 7 +
            _stringIEnumerableDeepComparer.GetHashCode(obj.StructuredMimeTypes?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 5 +
            _stringIEnumerableDeepComparer.GetHashCode(obj.IncludePatterns?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 3 +
            _stringIEnumerableDeepComparer.GetHashCode(obj.ExcludePatterns?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 2;
    }
    private static int GetVersionHashCode(string version)
    {
        if (string.IsNullOrEmpty(version)) return 0;
        if (Version.TryParse(version, out var parsedVersion))
        {
            if (parsedVersion.Major > 0)
                return parsedVersion.Major.GetHashCode();
            if (parsedVersion.Minor > 0)
                return parsedVersion.Minor.GetHashCode();
        }
        return 0;
    }
}
