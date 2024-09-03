using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.EqualityComparers;

internal class OpenApiSchemaReferenceComparer : IEqualityComparer<OpenApiSchema>
{
    public bool Equals(OpenApiSchema? x, OpenApiSchema? y)
    {
        if (string.IsNullOrEmpty(x?.Reference?.Id) || string.IsNullOrEmpty(y?.Reference?.Id)) return object.Equals(x, y);
        return string.Equals(x.Reference.Id, y.Reference.Id, StringComparison.OrdinalIgnoreCase);
    }
    public int GetHashCode([DisallowNull] OpenApiSchema obj)
    {
        var hash = new HashCode();
        if (!string.IsNullOrEmpty(obj.Reference?.Id))
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
