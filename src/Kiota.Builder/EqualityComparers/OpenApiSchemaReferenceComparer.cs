using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.EqualityComparers;

internal class OpenApiSchemaReferenceComparer : IEqualityComparer<OpenApiSchema>
{
    public bool Equals(OpenApiSchema? x, OpenApiSchema? y)
    {
        return x?.Reference.Id.Equals(y?.Reference.Id, StringComparison.OrdinalIgnoreCase) == true;
    }
    public int GetHashCode([DisallowNull] OpenApiSchema obj)
    {
        var hash = new HashCode();
        if (!string.IsNullOrEmpty(obj.Reference.Id))
        {
            hash.Add(obj.Reference.Id, StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            hash.Add(obj);
        }

        return hash.ToHashCode();
    }
}
