using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Nodes;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.Interfaces;

namespace Kiota.Builder.Validation;

internal class OpenApiSchemaComparer : IEqualityComparer<IOpenApiSchema>
{
    private readonly OpenApiDiscriminatorComparer discriminatorComparer;
    private readonly JsonNodeComparer jsonNodeComparer;

    public OpenApiSchemaComparer(
        OpenApiDiscriminatorComparer? discriminatorComparer = null,
        JsonNodeComparer? jsonNodeComparer = null)
    {
        this.discriminatorComparer = discriminatorComparer ?? new OpenApiDiscriminatorComparer();
        this.jsonNodeComparer = jsonNodeComparer ?? new JsonNodeComparer();
    }

    /// <inheritdoc/>
    public bool Equals(IOpenApiSchema? x, IOpenApiSchema? y)
    {
        // this workaround might result in collisions, however so far this has not been a problem
        // implemented this way to avoid stack overflow caused by schemas referencing themselves
        return x == null && y == null || x != null && y != null && GetHashCode(x) == GetHashCode(y);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] IOpenApiSchema obj)
    {
        var hash = new HashCode();
        GetHashCodeInternal(obj, [], ref hash);
        return hash.ToHashCode();
    }

    private void GetHashCodeInternal([DisallowNull] IOpenApiSchema obj, HashSet<IOpenApiSchema> visitedSchemas, ref HashCode hash)
    {
        if (obj is null) return;
        if (!visitedSchemas.Add(obj)) return;
        hash.Add(obj.Deprecated);
        if (obj.Discriminator is not null)
            hash.Add(obj.Discriminator, discriminatorComparer);
        if (obj.AdditionalProperties is not null)
            GetHashCodeInternal(obj.AdditionalProperties, visitedSchemas, ref hash);
        hash.Add(obj.AdditionalPropertiesAllowed);
        if (obj.Properties is not null)
            foreach (var prop in obj.Properties)
            {
                hash.Add(prop.Key, StringComparer.Ordinal);
                GetHashCodeInternal(prop.Value, visitedSchemas, ref hash);
            }
        if (obj.Default is not null)
            hash.Add(obj.Default, jsonNodeComparer);
        if (obj.Items is not null)
            GetHashCodeInternal(obj.Items, visitedSchemas, ref hash);
        if (obj.OneOf is not null)
            foreach (var schema in obj.OneOf)
            {
                GetHashCodeInternal(schema, visitedSchemas, ref hash);
            }
        if (obj.AnyOf is not null)
            foreach (var schema in obj.AnyOf)
            {
                GetHashCodeInternal(schema, visitedSchemas, ref hash);
            }
        if (obj.AllOf is not null)
            foreach (var schema in obj.AllOf)
            {
                GetHashCodeInternal(schema, visitedSchemas, ref hash);
            }
        hash.Add(obj.Format, StringComparer.OrdinalIgnoreCase);
        hash.Add(obj.Type);
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
    private static readonly IOrderedEnumerable<KeyValuePair<string, string>> defaultOrderedDictionary = new Dictionary<string, string>(0).OrderBy(x => x.Key, StringComparer.Ordinal);
    private static IOrderedEnumerable<KeyValuePair<string, string>> GetOrderedRequests(IDictionary<string, string>? mappings) =>
    mappings?.OrderBy(x => x.Key, StringComparer.Ordinal) ?? defaultOrderedDictionary;
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
internal class JsonNodeComparer : IEqualityComparer<JsonNode>
{
    /// <inheritdoc/>
    public bool Equals(JsonNode? x, JsonNode? y)
    {
        if (x is null || y is null) return object.Equals(x, y);
        // TODO: Can we use the OpenAPI.NET implementation of Equals?
        return x.GetValueKind() == y.GetValueKind() && string.Equals(x.ToJsonString(), y.ToJsonString(), StringComparison.OrdinalIgnoreCase);
    }
    /// <inheritdoc/>
    public int GetHashCode([DisallowNull] JsonNode obj)
    {
        var hash = new HashCode();
        if (obj == null) return hash.ToHashCode();
        hash.Add(obj.ToString());
        return hash.ToHashCode();
    }
}
