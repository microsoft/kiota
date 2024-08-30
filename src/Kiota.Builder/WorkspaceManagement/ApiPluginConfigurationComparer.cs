using System;
using System.Collections.Generic;
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
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        if (obj.AuthType is { } authType)
        {
            hash.Add(authType, StringComparer.OrdinalIgnoreCase);
        }
        hash.Add(0);
        if (obj.AuthReferenceId is { } referenceId)
        {
            hash.Add(referenceId, StringComparer.OrdinalIgnoreCase);
        }
        return
            _stringIEnumerableDeepComparer.GetHashCode(obj.Types?.Order(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>()) * 11 +
            hash.ToHashCode() +
            base.GetHashCode(obj);
    }
}
