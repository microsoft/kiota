using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Kiota.Builder.Lock;
using Microsoft.Plugins.Manifest;

namespace Kiota.Builder.Plugins;

internal class OpenAPIRuntimeComparer : IEqualityComparer<OpenApiRuntime>
{
    public bool EvaluateFunctions
    {
        get; init;
    }
    private static readonly StringIEnumerableDeepComparer _stringIEnumerableDeepComparer = new();
    private static readonly AuthComparer _authComparer = new();
    private static readonly OpenApiRuntimeSpecComparer _openApiRuntimeSpecComparer = new();
    /// <inheritdoc/>
    public bool Equals(OpenApiRuntime? x, OpenApiRuntime? y)
    {
        return x == null && y == null || x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] OpenApiRuntime obj)
    {
        if (obj == null) return 0;
        return (EvaluateFunctions ? _stringIEnumerableDeepComparer.GetHashCode(obj.RunForFunctions ?? Enumerable.Empty<string>()) * 7 : 0) +
            (obj.Spec is null ? 0 : _openApiRuntimeSpecComparer.GetHashCode(obj.Spec) * 5) +
            (obj.Auth is null ? 0 : _authComparer.GetHashCode(obj.Auth) * 3);
    }
}
