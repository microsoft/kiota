using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi;

namespace Kiota.Builder.EqualityComparers;

internal class OpenApiSchemaReferenceComparer(StringComparer? stringComparer = null) : IEqualityComparer<IOpenApiSchema>
{
    private readonly StringComparer _stringComparer = stringComparer ?? StringComparer.OrdinalIgnoreCase;
    public bool Equals(IOpenApiSchema? x, IOpenApiSchema? y)
    {
        if (x is not OpenApiSchemaReference xRef || y is not OpenApiSchemaReference yRef) return object.Equals(x, y);
        return _stringComparer.Equals(xRef.Reference.Id, yRef.Reference.Id);
    }
    public int GetHashCode([DisallowNull] IOpenApiSchema obj)
    {
        if (obj is not OpenApiSchemaReference objRef) return obj.GetHashCode();
        var hash = new HashCode();
        if (!string.IsNullOrEmpty(objRef.Reference?.Id))
        {
            hash.Add(objRef.Reference.Id, _stringComparer);
        }
        else
        {
            hash.Add(objRef);
        }

        return hash.ToHashCode();
    }
}
