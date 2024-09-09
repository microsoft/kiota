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
    private readonly StringIEnumerableDeepComparer _stringIEnumerableDeepComparer;
    private readonly StringComparer _stringComparer;

    public ApiPluginConfigurationComparer(StringIEnumerableDeepComparer? stringIEnumerableDeepComparer = null,
        StringComparer? stringComparer = null)
    {
        _stringComparer = stringComparer ?? StringComparer.OrdinalIgnoreCase;
        _stringIEnumerableDeepComparer =
            stringIEnumerableDeepComparer ?? new StringIEnumerableDeepComparer(_stringComparer);
    }

    public override bool Equals(ApiPluginConfiguration? x, ApiPluginConfiguration? y)
    {
        if (x is null || y is null) return object.Equals(x, y);
        return x.Types.SequenceEqual(y.Types, _stringComparer) && base.Equals(x, y);
    }

    /// <inheritdoc/>
    public override int GetHashCode([DisallowNull] ApiPluginConfiguration obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        hash.Add(obj.AuthType, _stringComparer);
        hash.Add(obj.AuthReferenceId, _stringComparer);
        hash.Add(obj.Types, _stringIEnumerableDeepComparer);
        hash.Add(base.GetHashCode(obj));
        return hash.ToHashCode();
    }
}
