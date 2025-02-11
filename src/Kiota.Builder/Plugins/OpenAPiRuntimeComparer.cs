using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Kiota.Builder.Lock;
using Microsoft.DeclarativeAgents.Manifest;

namespace Kiota.Builder.Plugins;

internal class OpenAPIRuntimeComparer : IEqualityComparer<OpenApiRuntime>
{
    public bool EvaluateFunctions
    {
        get; init;
    }
    private readonly StringIEnumerableDeepComparer _stringIEnumerableDeepComparer;
    private readonly AuthComparer _authComparer;
    private readonly OpenApiRuntimeSpecComparer _openApiRuntimeSpecComparer;

    public OpenAPIRuntimeComparer(
        AuthComparer? authComparer = null,
        OpenApiRuntimeSpecComparer? openApiRuntimeSpecComparer = null,
        StringIEnumerableDeepComparer? stringIEnumerableDeepComparer = null)
    {
        _authComparer = authComparer ?? new AuthComparer();
        _openApiRuntimeSpecComparer = openApiRuntimeSpecComparer ?? new OpenApiRuntimeSpecComparer();
        _stringIEnumerableDeepComparer = stringIEnumerableDeepComparer ?? new StringIEnumerableDeepComparer();
    }

    /// <inheritdoc/>
    public bool Equals(OpenApiRuntime? x, OpenApiRuntime? y)
    {
        // Unit tests check for this
        if (x is null || y is null) return object.Equals(x, y);
        bool functionsEqual = !EvaluateFunctions || _stringIEnumerableDeepComparer.Equals(x.RunForFunctions, y.RunForFunctions);
        return functionsEqual && _openApiRuntimeSpecComparer.Equals(x.Spec, y.Spec) && _authComparer.Equals(x.Auth, y.Auth);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] OpenApiRuntime obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        if (EvaluateFunctions) hash.Add(obj.RunForFunctions, _stringIEnumerableDeepComparer);
        hash.Add(obj.Spec, _openApiRuntimeSpecComparer);
        hash.Add(obj.Auth, _authComparer);
        return hash.ToHashCode();
    }
}
