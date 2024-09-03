using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.EqualityComparers;

internal class OpenApiSchemaReferenceComparer(StringComparer? stringComparer = null) : IEqualityComparer<OpenApiSchema>
{
    private readonly StringComparer _stringComparer = stringComparer ?? StringComparer.OrdinalIgnoreCase;
    public bool Equals(OpenApiSchema? x, OpenApiSchema? y)
    {
        if (string.IsNullOrEmpty(x?.Reference?.Id) || string.IsNullOrEmpty(y?.Reference?.Id)) return object.Equals(x, y);
        return _stringComparer.Equals(x.Reference.Id, y.Reference.Id);
    }
    public int GetHashCode([DisallowNull] OpenApiSchema obj)
    {
        var hash = new HashCode();
        if (!string.IsNullOrEmpty(obj.Reference?.Id))
        {
            hash.Add(obj.Reference.Id, _stringComparer);
        }
        else
        {
            hash.Add(obj);
        }

        return hash.ToHashCode();
    }
}
