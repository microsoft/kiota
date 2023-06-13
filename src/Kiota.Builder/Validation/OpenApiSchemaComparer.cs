using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Kiota.Builder.Extensions;
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
                (obj.Properties.Select(x => GetHashCodeInternal(x.Value, visitedSchemas) + x.Key.GetHashCode(StringComparison.Ordinal)).SumUnchecked() * 29) +
                openApiAnyComparer.GetHashCode(obj.Default) * 23 +
                (GetHashCodeInternal(obj.Items, visitedSchemas) * 19) +
                (obj.OneOf.Select(x => GetHashCodeInternal(x, visitedSchemas)).SumUnchecked() * 17) +
                (obj.AnyOf.Select(x => GetHashCodeInternal(x, visitedSchemas)).SumUnchecked() * 11) +
                (obj.AllOf.Select(x => GetHashCodeInternal(x, visitedSchemas)).SumUnchecked() * 7) +
                (string.IsNullOrEmpty(obj.Format) ? 0 : obj.Format.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 5 +
                (string.IsNullOrEmpty(obj.Type) ? 0 : obj.Type.GetHashCode(StringComparison.OrdinalIgnoreCase)) * 3 +
                (string.IsNullOrEmpty(obj.Title) ? 0 : obj.Title.GetHashCode(StringComparison.Ordinal)) * 2;
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
        return (string.IsNullOrEmpty(obj.PropertyName) ? 0 : obj.PropertyName.GetHashCode(StringComparison.Ordinal)) * 89 +
            (obj.Mapping?.Select(static x => x.Key.GetHashCode(StringComparison.Ordinal) + (string.IsNullOrEmpty(x.Value) ? 0 : x.Value.GetHashCode(StringComparison.Ordinal))).SumUnchecked() ?? 0) * 83;
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
        return stringValue.GetHashCode(StringComparison.Ordinal) * 97;
    }
}
