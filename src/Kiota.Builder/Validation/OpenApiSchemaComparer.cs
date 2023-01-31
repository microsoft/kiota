using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Validation;

internal class OpenApiSchemaComparer : IEqualityComparer<OpenApiSchema>
{
    private static readonly OpenApiDiscriminatorComparer discriminatorComparer = new();
    private static readonly OpenApiAnyComparer openApiAnyComparer = new();
    /// <inheritdoc/>
    public bool Equals(OpenApiSchema? x, OpenApiSchema? y)
    {
        return x == null && y == null || x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] OpenApiSchema obj)
    {
        return GetHashCodeInternal(obj, new());
    }
    private static int GetHashCodeInternal([DisallowNull] OpenApiSchema obj, HashSet<OpenApiSchema> visitedSchemas)
    {
        if (obj == null) return 0;
        if (visitedSchemas.Contains(obj)) return 0;
        visitedSchemas.Add(obj);
        unchecked
        {
            return
                Convert.ToInt32(obj.Deprecated) * 47 +
                Convert.ToInt32(obj.Nullable) * 43 +
                discriminatorComparer.GetHashCode(obj.Discriminator) * 41 +
                (GetHashCodeInternal(obj.AdditionalProperties, visitedSchemas) * 37) +
                Convert.ToInt32(obj.AdditionalPropertiesAllowed) * 31 +
                (SumUnchecked(obj.Properties.Select(x => GetHashCodeInternal(x.Value, visitedSchemas) + x.Key.GetHashCode())) * 29) +
                openApiAnyComparer.GetHashCode(obj.Default) * 23 +
                (GetHashCodeInternal(obj.Items, visitedSchemas) * 19) +
                (SumUnchecked(obj.OneOf.Select(x => GetHashCodeInternal(x, visitedSchemas))) * 17) +
                (SumUnchecked(obj.AnyOf.Select(x => GetHashCodeInternal(x, visitedSchemas))) * 11) +
                (SumUnchecked(obj.AllOf.Select(x => GetHashCodeInternal(x, visitedSchemas))) * 7) +
                (string.IsNullOrEmpty(obj.Format) ? 0 : obj.Format.GetHashCode()) * 5 +
                (string.IsNullOrEmpty(obj.Type) ? 0 : obj.Type.GetHashCode()) * 3 +
                (string.IsNullOrEmpty(obj.Title) ? 0 : obj.Title.GetHashCode()) * 2;
        }
        /**
         ignored properties since they don't impact generation:
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

internal class OpenApiDiscriminatorComparer : IEqualityComparer<OpenApiDiscriminator>
{
    /// <inheritdoc/>
    public bool Equals(OpenApiDiscriminator? x, OpenApiDiscriminator? y)
    {
        return x == null && y == null || x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] OpenApiDiscriminator obj)
    {
        if (obj == null) return 0;
        return (string.IsNullOrEmpty(obj.PropertyName) ? 0 : obj.PropertyName.GetHashCode()) * 89 +
            (obj.Mapping?.Select(static x => x.Key.GetHashCode() + (string.IsNullOrEmpty(x.Value) ? 0 : x.Value.GetHashCode())).Sum() ?? 0) * 83;
    }
}
internal class OpenApiAnyComparer : IEqualityComparer<IOpenApiAny>
{
    /// <inheritdoc/>
    public bool Equals(IOpenApiAny? x, IOpenApiAny? y)
    {
        return x == null && y == null || x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] IOpenApiAny obj)
    {
        if (obj == null || obj.ToString() is not string stringValue || string.IsNullOrEmpty(stringValue)) return 0;
        return stringValue.GetHashCode() * 97;
    }
}
