using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiota.Builder.Lock;

/// <summary>
/// Compares two <see cref="KiotaLock"/> instances.
/// </summary>
public class KiotaLockComparer : IEqualityComparer<KiotaLock>
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public bool Equals(KiotaLock x, KiotaLock y)
    {
        return GetHashCode(x) == GetHashCode(y);
    }
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public int GetHashCode(KiotaLock obj)
    {
        if (obj == null) return 0;
        return GetVersionHashCode(obj.KiotaVersion) * 43 +
            GetVersionHashCode(obj.LockFileVersion) * 41 +
            (obj.DescriptionLocation?.GetHashCode() ?? 0) * 37 +
            (obj.DescriptionHash?.GetHashCode() ?? 0) * 31 +
            (obj.ClientClassName?.GetHashCode() ?? 0) * 29 +
            (obj.ClientNamespaceName?.GetHashCode() ?? 0) * 23 +
            (obj.Language?.GetHashCode() ?? 0) * 19 +
            obj.UsesBackingStore.GetHashCode() * 17 +
            obj.IncludeAdditionalData.GetHashCode() * 13 +
            string.Join(",", obj.Serializers?.OrderBy(static x => x, StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()).GetHashCode() * 11 +
            string.Join(",", obj.Deserializers?.OrderBy(static x => x, StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()).GetHashCode() * 7 +
            string.Join(",", obj.StructuredMimeTypes?.OrderBy(static x => x, StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()).GetHashCode() * 5 +
            string.Join(",", obj.IncludePatterns?.OrderBy(static x => x, StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()).GetHashCode() * 3 +
            string.Join(",", obj.ExcludePatterns?.OrderBy(static x => x, StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()).GetHashCode() * 2;
    }
    private static int GetVersionHashCode(string version) {
        if(string.IsNullOrEmpty(version)) return 0;
        if(Version.TryParse(version, out var parsedVersion)) {
            if (parsedVersion.Major > 0)
                return parsedVersion.Major.GetHashCode();
            if (parsedVersion.Minor > 0)
                return parsedVersion.Minor.GetHashCode();
        }
        return 0;
    }
}
