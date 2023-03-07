using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Validation;

internal class PropertyOpenApiSchemaComparer : IEqualityComparer<(string, OpenApiSchema)>
{
    private static readonly OpenApiDiscriminatorComparer discriminatorComparer = new();
    /// <inheritdoc/>
    public bool Equals((string, OpenApiSchema) x, (string, OpenApiSchema) y)
    {
        return (x.Item1 == null && y.Item1 == null && x.Item2 == null && y.Item2 == null) ||
        x.Item1 != null && y.Item1 != null && x.Item2 != null && y.Item2 != null && GetHashCode(x) == GetHashCode(y);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] (string, OpenApiSchema) body)
    {
        return string.GetHashCode(body.Item1) + GetHashCodeInternal(body.Item2, new());
    }
    private static int GetHashCodeInternal([DisallowNull] OpenApiSchema obj, HashSet<OpenApiSchema> visitedSchemas)
    {
        if (obj == null) return 0;
        if (visitedSchemas.Contains(obj)) return 0;
        visitedSchemas.Add(obj);
        unchecked
        {
            return
                Convert.ToInt32(obj.Nullable) * 43 +
                discriminatorComparer.GetHashCode(obj.Discriminator) * 41 +
                (GetHashCodeInternal(obj.AdditionalProperties, visitedSchemas) * 37) +
                Convert.ToInt32(obj.AdditionalPropertiesAllowed) * 31 +
                (SumUnchecked(obj.Properties.Select(x => GetHashCodeInternal(x.Value, visitedSchemas) + x.Key.GetHashCode())) * 29) +
                (GetHashCodeInternal(obj.Items, visitedSchemas) * 19) +
                (SumUnchecked(obj.OneOf.Select(x => GetHashCodeInternal(x, visitedSchemas))) * 17) +
                (SumUnchecked(obj.AnyOf.Select(x => GetHashCodeInternal(x, visitedSchemas))) * 11) +
                (SumUnchecked(obj.AllOf.Select(x => GetHashCodeInternal(x, visitedSchemas))) * 7) +
                (string.IsNullOrEmpty(obj.Format) ? 0 : obj.Format.GetHashCode()) * 5 +
                (string.IsNullOrEmpty(obj.Type) ? 0 : obj.Type.GetHashCode()) * 3 +
                (string.IsNullOrEmpty(obj.Title) ? 0 : obj.Title.GetHashCode()) * 2;
        }
        /**
         ignored properties since they don't impact the resulting type:
         - Deprecated
         - Default
         - Description
         - Example
         - ExclusiveMaximum
         - ExclusiveMinimum
         - External docs
         - Maximum
         - MaxItems
         - MaxLength
         - Minimum
         - MinItems
         - MinLength
         - MultipleOf
         - Not
         - OpenApiReference
         - Pattern
         - ReadOnly
         - Required
         - UniqueItems
         - UnresolvedReference
         - WriteOnly
         - Xml
        */
    }
    private static int SumUnchecked(IEnumerable<int> values)
    {
        unchecked
        {
            return values.Aggregate(0, static (acc, x) => acc + x);
        }
    }
}
