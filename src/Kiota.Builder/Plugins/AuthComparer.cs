using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.DeclarativeAgents.Manifest;

namespace Kiota.Builder.Plugins;

internal class AuthComparer(StringComparer? stringComparer = null) : IEqualityComparer<Auth?>
{
    private readonly StringComparer _stringComparer = stringComparer ?? StringComparer.OrdinalIgnoreCase;
    /// <inheritdoc/>
    public bool Equals(Auth? x, Auth? y)
    {
        if (x is null || y is null) return object.Equals(x, y);
        return x switch
        {
            AnonymousAuth when y is AnonymousAuth => true,
            ApiKeyPluginVault x0 when y is ApiKeyPluginVault y0 => _stringComparer.Equals(x0.ReferenceId,
                y0.ReferenceId),
            OAuthPluginVault x1 when y is OAuthPluginVault y1 => _stringComparer.Equals(x1.ReferenceId, y1.ReferenceId),
            EntraOnBehalfOf x2 when y is EntraOnBehalfOf y2 => (x2.Scopes ?? []).SequenceEqual(y2.Scopes ?? []),
            _ => false
        };
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] Auth obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        hash.Add(obj.Type, _stringComparer);
        switch (obj)
        {
            case ApiKeyPluginVault o0:
                hash.Add(o0.ReferenceId, _stringComparer);
                break;
            case OAuthPluginVault o1:
                hash.Add(o1.ReferenceId, _stringComparer);
                break;
            case EntraOnBehalfOf o2:
                foreach (var scope in o2.Scopes ?? [])
                {
                    hash.Add(scope, _stringComparer);
                }
                break;
        }

        return hash.ToHashCode();
    }
}
