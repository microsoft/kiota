using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Kiota.Builder.Lock;

namespace Kiota.Builder.WorkspaceManagement;
/// <summary>
/// Compares two <see cref="ApiPluginConfiguration"/> instances.
/// </summary>
public class ApiPluginConfigurationComparer : BaseApiConsumerConfigurationComparer<ApiPluginConfiguration>
{
    private static readonly StringIEnumerableDeepComparer _stringIEnumerableDeepComparer = new();
    /// <inheritdoc/>
    public override int GetHashCode([DisallowNull] ApiPluginConfiguration obj)
    {
        if (obj == null) return 0;
        return
            _stringIEnumerableDeepComparer.GetHashCode(obj.Types?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 11 +
            string.GetHashCode(obj.AuthType, StringComparison.OrdinalIgnoreCase) +
            0xff.GetHashCode() + // Separate the 2 strings. avoids "autha", "uthref" vs "auth", "authref" having the same hash
            string.GetHashCode(obj.AuthReferenceId, StringComparison.OrdinalIgnoreCase) +
            base.GetHashCode(obj);
    }
}
