using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.EqualityComparers;

internal class OpenApiSchemaReferenceComparer : IEqualityComparer<OpenApiSchema>
{
    public bool Equals(OpenApiSchema? x, OpenApiSchema? y)
    {
        return (x, y) switch
        {
            (null, null) => true,
            (null, _) => false,
            (_, null) => false,
            _ => GetHashCode(x) == GetHashCode(y),
        };
    }
    public int GetHashCode([DisallowNull] OpenApiSchema obj)
    {
        return obj.Reference is not null && !string.IsNullOrEmpty(obj.Reference.Id) ? obj.Reference.Id.GetHashCode(StringComparison.OrdinalIgnoreCase) : obj.GetHashCode();
    }
}
