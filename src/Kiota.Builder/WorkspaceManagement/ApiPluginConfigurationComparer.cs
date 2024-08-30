﻿using System;
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
    public override bool Equals(ApiPluginConfiguration? x, ApiPluginConfiguration? y)
    {
        if (x is null || y is null) return x?.Equals(y) == true;
        return x.Types.SequenceEqual(y.Types, StringComparer.OrdinalIgnoreCase) && base.Equals(x, y);
    }

    /// <inheritdoc/>
    public override int GetHashCode([DisallowNull] ApiPluginConfiguration obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        hash.Add(obj.Types, _stringIEnumerableDeepComparer);
        // TODO: can this be combined in a better way?
        return hash.ToHashCode() * 11 + base.GetHashCode(obj);
    }
}
