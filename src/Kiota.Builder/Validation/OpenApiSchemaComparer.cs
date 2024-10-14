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
    private readonly OpenApiDiscriminatorComparer discriminatorComparer;
    private readonly OpenApiAnyComparer openApiAnyComparer;
    private readonly KeyValueComparer<string, OpenApiSchema> schemaMapComparer;

    public OpenApiSchemaComparer(
        OpenApiDiscriminatorComparer? discriminatorComparer = null,
        OpenApiAnyComparer? openApiAnyComparer = null,
        KeyValueComparer<string, OpenApiSchema>? schemaMapComparer = null)
    {
        this.discriminatorComparer = discriminatorComparer ?? new OpenApiDiscriminatorComparer();
        this.openApiAnyComparer = openApiAnyComparer ?? new OpenApiAnyComparer();
        this.schemaMapComparer = schemaMapComparer ?? new KeyValueComparer<string, OpenApiSchema>(StringComparer.Ordinal, this);
    }

    /// <inheritdoc/>
    public bool Equals(OpenApiSchema? x, OpenApiSchema? y)
    {
        // this workaround might result in collisions, however so far this has not been a problem
        // implemented this way to avoid stack overflow caused by schemas referencing themselves
        return x == null && y == null || x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] OpenApiSchema obj)
    {
        var hash = new HashCode();
        GetHashCodeInternal(obj, [], ref hash);
        return hash.ToHashCode();
    }

    private void GetHashCodeInternal([DisallowNull] OpenApiSchema obj, HashSet<OpenApiSchema> visitedSchemas, ref HashCode hash)
    {
        if (obj is null) return;
        if (!visitedSchemas.Add(obj)) return;
        hash.Add(obj.Deprecated);
        hash.Add(obj.Nullable);
        hash.Add(obj.Discriminator, discriminatorComparer);
        GetHashCodeInternal(obj.AdditionalProperties, visitedSchemas, ref hash);
        hash.Add(obj.AdditionalPropertiesAllowed);
        foreach (var prop in obj.Properties)
        {
            hash.Add(prop.Key, StringComparer.Ordinal);
            GetHashCodeInternal(prop.Value, visitedSchemas, ref hash);
        }
        hash.Add(obj.Default, openApiAnyComparer);
        GetHashCodeInternal(obj.Items, visitedSchemas, ref hash);
        foreach (var schema in obj.OneOf)
        {
            GetHashCodeInternal(schema, visitedSchemas, ref hash);
        }
        foreach (var schema in obj.AnyOf)
        {
            GetHashCodeInternal(schema, visitedSchemas, ref hash);
        }
        foreach (var schema in obj.AllOf)
        {
            GetHashCodeInternal(schema, visitedSchemas, ref hash);
        }
        hash.Add(obj.Format, StringComparer.OrdinalIgnoreCase);
        hash.Add(obj.Type, StringComparer.OrdinalIgnoreCase);
        hash.Add(obj.Title, StringComparer.Ordinal);
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

internal class KeyValueComparer<K, V>(
    IEqualityComparer<K>? keyComparer = null,
    IEqualityComparer<V>? valueComparer = null)
    : IEqualityComparer<KeyValuePair<K, V>>
{
    private readonly IEqualityComparer<K> _keyComparer = keyComparer ?? EqualityComparer<K>.Default;
    private readonly IEqualityComparer<V> _valueComparer = valueComparer ?? EqualityComparer<V>.Default;

    public bool Equals(KeyValuePair<K, V> x, KeyValuePair<K, V> y)
    {
        return _keyComparer.Equals(x.Key, y.Key) && _valueComparer.Equals(x.Value, y.Value);
    }

    public int GetHashCode(KeyValuePair<K, V> obj)
    {
        var hash = new HashCode();
        hash.Add(obj.Key, _keyComparer);
        hash.Add(obj.Value, _valueComparer);
        return hash.ToHashCode();
    }
}

internal class OpenApiDiscriminatorComparer : IEqualityComparer<OpenApiDiscriminator>
{
    private static readonly KeyValueComparer<string, string> mappingComparer = new(StringComparer.Ordinal, StringComparer.Ordinal);
    /// <inheritdoc/>
    public bool Equals(OpenApiDiscriminator? x, OpenApiDiscriminator? y)
    {
        if (x is null || y is null) return object.Equals(x, y);
        return x.PropertyName.EqualsIgnoreCase(y.PropertyName) && GetOrderedRequests(x.Mapping).SequenceEqual(GetOrderedRequests(y.Mapping), mappingComparer);
    }
    private static IOrderedEnumerable<KeyValuePair<string, string>> GetOrderedRequests(IDictionary<string, string> mappings) =>
    mappings.OrderBy(x => x.Key, StringComparer.Ordinal);
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] OpenApiDiscriminator obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        hash.Add(obj.PropertyName);
        foreach (var mapping in GetOrderedRequests(obj.Mapping))
        {
            hash.Add(mapping, mappingComparer);
        }
        return hash.ToHashCode();
    }
}
internal class OpenApiAnyComparer : IEqualityComparer<OpenApiAny>
{
    /// <inheritdoc/>
    public bool Equals(OpenApiAny? x, OpenApiAny? y)
    {
        if (x is null || y is null) return object.Equals(x, y);
        // TODO: Can we use the OpenAPI.NET implementation of Equals?
        return x.Node.GetValueKind() == y.Node.GetValueKind() && string.Equals(x.Node.ToString(), y.Node.ToString(), StringComparison.OrdinalIgnoreCase);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] OpenApiAny obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        hash.Add(obj.ToString());
        return hash.ToHashCode();
    }
}
