using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Kiota.Builder.Lock;
using Microsoft.Plugins.Manifest;

namespace Kiota.Builder.Plugins;

internal class OpenAPIRuntimeComparer : IEqualityComparer<OpenAPIRuntime>
{
    public bool EvaluateFunctions
    {
        get; init;
    }
    private static readonly StringIEnumerableDeepComparer _stringIEnumerableDeepComparer = new();
    private static readonly AuthComparer _authComparer = new();
    /// <inheritdoc/>
    public bool Equals(OpenAPIRuntime? x, OpenAPIRuntime? y)
    {
        return x == null && y == null || x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] OpenAPIRuntime obj)
    {
        if (obj == null) return 0;
        return (EvaluateFunctions ? _stringIEnumerableDeepComparer.GetHashCode(obj.RunForFunctions ?? Enumerable.Empty<string>()) * 7 : 0) +
            obj.Spec.Select(static x => StringComparer.Ordinal.GetHashCode($"{x.Key}:{x.Value}")).Aggregate(0, (acc, next) => acc + next) * 5 +
            (obj.Auth is null ? 0 : _authComparer.GetHashCode(obj.Auth) * 3);
    }
}
