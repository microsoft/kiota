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
            _ when x.Reference is not null && y.Reference is not null => x.Reference.Id.Equals(y.Reference.Id, StringComparison.OrdinalIgnoreCase),
            _ => x == y,
        };
    }
    public int GetHashCode([DisallowNull] OpenApiSchema obj)
    {
        return obj.Reference is not null && !string.IsNullOrEmpty(obj.Reference.Id) ? obj.Reference.Id.GetHashCode(StringComparison.OrdinalIgnoreCase) : obj.GetHashCode();
    }
}
