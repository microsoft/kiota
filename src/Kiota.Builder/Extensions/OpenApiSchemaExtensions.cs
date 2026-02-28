using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Kiota.Builder.OpenApiExtensions;
using Microsoft.OpenApi;

namespace Kiota.Builder.Extensions;

public static class OpenApiSchemaExtensions
{
    private static readonly Func<IOpenApiSchema, IList<IOpenApiSchema>> classNamesFlattener = x =>
    (x.AnyOf ?? Enumerable.Empty<IOpenApiSchema>()).Union(x.AllOf ?? []).Union(x.OneOf ?? []).ToList();
    public static IEnumerable<string> GetSchemaNames(this IOpenApiSchema schema, bool directOnly = false)
    {
        if (schema == null)
            return [];
        if (!directOnly && schema.Items != null)
            return schema.Items.GetSchemaNames();
        if (schema.GetReferenceId() is string refId && !string.IsNullOrEmpty(refId))
            return [refId.Split('/')[^1].Split('.')[^1]];
        if (!directOnly && schema.AnyOf is { Count: > 0 })
            return schema.AnyOf.FlattenIfRequired(classNamesFlattener);
        if (!directOnly && schema.AllOf is { Count: > 0 })
            return schema.AllOf.FlattenIfRequired(classNamesFlattener);
        if (!directOnly && schema.OneOf is { Count: > 0 })
            return schema.OneOf.FlattenIfRequired(classNamesFlattener);
        return [];
    }
    internal static string? GetReferenceId(this IOpenApiSchema schema)
    {
        return schema switch
        {
            OpenApiSchemaReference reference => reference.Reference?.Id,
            OpenApiSchema s when s.GetMergedSchemaOriginalReferenceId() is string originalReferenceId => originalReferenceId,
            _ => null,
        };
    }
    internal static IEnumerable<IOpenApiSchema> FlattenSchemaIfRequired(this IList<IOpenApiSchema>? schemas, Func<IOpenApiSchema, IList<IOpenApiSchema>?> subsequentGetter)
    {
        if (schemas is null) return [];
        return schemas.Count == 1 && !schemas[0].HasAnyProperty() && string.IsNullOrEmpty(schemas[0].GetReferenceId()) ?
                    schemas.FlattenEmptyEntries(subsequentGetter, 1) :
                    schemas;
    }
    private static IEnumerable<string> FlattenIfRequired(this IList<IOpenApiSchema> schemas, Func<IOpenApiSchema, IList<IOpenApiSchema>> subsequentGetter)
    {
        return schemas.FlattenSchemaIfRequired(subsequentGetter).SelectMany(static x => x.GetSchemaNames());
    }

    public static string GetSchemaName(this IOpenApiSchema schema, bool directOnly = false)
    {
        return schema.GetSchemaNames(directOnly).LastOrDefault()?.TrimStart('$') ?? string.Empty;// OData $ref
    }

    public static bool IsReferencedSchema(this IOpenApiSchema schema)
    {
        return schema switch
        {
            OpenApiSchemaReference => true,
            _ => false,
        };
    }

    public static bool IsArray(this IOpenApiSchema? schema)
    {
        return schema is { Type: JsonSchemaType.Array or (JsonSchemaType.Array | JsonSchemaType.Null) } && schema.Items is not null &&
            (schema.Items.IsComposedEnum() ||
            schema.Items.IsEnum() ||
            schema.Items.IsSemanticallyMeaningful() ||
            FlattenEmptyEntries([schema.Items], static x => (x.AnyOf ?? []).Union(x.AllOf ?? []).Union(x.OneOf ?? []).ToList(), 1).FirstOrDefault() is IOpenApiSchema flat && flat.IsSemanticallyMeaningful());
    }

    public static bool IsObjectType(this IOpenApiSchema? schema)
    {
        return schema is { Type: JsonSchemaType.Object or (JsonSchemaType.Object | JsonSchemaType.Null) };
    }
    public static bool HasAnyProperty(this IOpenApiSchema? schema)
    {
        return schema?.Properties is { Count: > 0 };
    }
    public static bool IsInclusiveUnion(this IOpenApiSchema? schema, uint exclusiveMinimumNumberOfEntries = 1)
    {
        return schema?.AnyOf?.Count(static x => IsSemanticallyMeaningful(x, true)) > exclusiveMinimumNumberOfEntries;
        // so we don't consider any of object/nullable as a union type
    }

    public static bool IsInherited(this IOpenApiSchema? schema)
    {
        if (schema is null) return false;
        var meaningfulMemberSchemas = schema.AllOf.FlattenSchemaIfRequired(static x => x.AllOf)
                                                                .Where(static x => x.IsSemanticallyMeaningful(ignoreEnums: true, ignoreArrays: true, ignoreType: true))
                                                                // the next line ensures the meaningful schema are objects as it won't make sense inheriting from a primitive despite it being meaningful.
                                                                .Where(static x => string.IsNullOrEmpty(x.GetReferenceId()) || x.Type is null || !x.Type.HasValue || (x.Type.Value & JsonSchemaType.Object) is JsonSchemaType.Object)
                                                                .ToArray();
        var isRootSchemaMeaningful = schema.IsSemanticallyMeaningful(ignoreEnums: true, ignoreArrays: true, ignoreType: true);
        return meaningfulMemberSchemas.Count(static x => !string.IsNullOrEmpty(x.GetReferenceId())) == 1 &&
            (meaningfulMemberSchemas.Count(static x => string.IsNullOrEmpty(x.GetReferenceId())) == 1 ||
            isRootSchemaMeaningful);
    }

    internal static IOpenApiSchema? MergeAllOfSchemaEntries(this IOpenApiSchema? schema, HashSet<IOpenApiSchema>? schemasToExclude = default, Func<IOpenApiSchema, bool>? filter = default)
    {
        return schema.MergeIntersectionSchemaEntries(schemasToExclude, true, filter);
    }

    internal static IOpenApiSchema? MergeInclusiveUnionSchemaEntries(this IOpenApiSchema? schema)
    {
        if (schema is null || !schema.IsInclusiveUnion(0)) return null;
        var result = schema.GetSchemaOrTargetShallowCopy();
        if (result.AnyOf is null || schema.AnyOf is null) return result;
        result.AnyOf.Clear();
        result.TryAddProperties(schema.AnyOf.Where(static x => x.Properties is not null).SelectMany(static x => x.Properties!));
        schema.AddOriginalReferenceIdExtension(result);
        return result;
    }

    internal static IOpenApiSchema? MergeExclusiveUnionSchemaEntries(this IOpenApiSchema? schema)
    {
        if (schema is null || !schema.IsExclusiveUnion(0)) return null;
        var result = schema.GetSchemaOrTargetShallowCopy();
        if (result.OneOf is null || schema.OneOf is null) return result;
        result.OneOf.Clear();
        result.TryAddProperties(schema.OneOf.Where(static x => x.Properties is not null).SelectMany(static x => x.Properties!));
        schema.AddOriginalReferenceIdExtension(result);
        return result;
    }

    internal static IOpenApiSchema? MergeSingleInclusiveUnionInheritanceOrIntersectionSchemaEntries(this IOpenApiSchema? schema)
    {
        if (schema is not null
            && schema.IsInclusiveUnion(0)
            && schema.AnyOf.OnlyOneOrDefault() is { Properties: not null, AllOf: not null } subSchema
            && (subSchema.IsInherited() || subSchema.IsIntersection()))
        {
            var result = schema.GetSchemaOrTargetShallowCopy();
            result.AnyOf?.Clear();
            result.TryAddProperties(subSchema.Properties);
            result.AllOf ??= [];
            result.AllOf.AddRange(subSchema.AllOf);
            schema.AddOriginalReferenceIdExtension(result);
            return result;
        }

        return null;
    }

    internal static IOpenApiSchema? MergeSingleExclusiveUnionInheritanceOrIntersectionSchemaEntries(this IOpenApiSchema? schema)
    {
        if (schema is not null
            && schema.IsExclusiveUnion(0)
            && schema.OneOf.OnlyOneOrDefault() is { Properties: not null, AllOf: not null } subSchema
            && (subSchema.IsInherited() || subSchema.IsIntersection()))
        {
            var result = schema.GetSchemaOrTargetShallowCopy();
            result.OneOf?.Clear();
            result.TryAddProperties(subSchema.Properties);
            result.AllOf ??= [];
            result.AllOf.AddRange(subSchema.AllOf);
            schema.AddOriginalReferenceIdExtension(result);
            return result;
        }

        return null;
    }

    private static OpenApiSchema GetSchemaOrTargetShallowCopy(this IOpenApiSchema? schema)
    {
        return schema switch
        {
            OpenApiSchema oas => (OpenApiSchema)oas.CreateShallowCopy(),
            OpenApiSchemaReference oasr when oasr.Target is OpenApiSchema target => (OpenApiSchema)target.CreateShallowCopy(),
            OpenApiSchemaReference => throw new InvalidOperationException("The schema reference is not resolved"),
            _ => throw new InvalidOperationException("The schema type is not supported")
        };
    }

    internal static IOpenApiSchema? MergeIntersectionSchemaEntries(this IOpenApiSchema? schema, HashSet<IOpenApiSchema>? schemasToExclude = default, bool overrideIntersection = false, Func<IOpenApiSchema, bool>? filter = default)
    {
        if (schema is null) return null;
        if (schema.AllOf is null || !schema.IsIntersection() && !overrideIntersection) return schema;

        var result = schema.GetSchemaOrTargetShallowCopy();
        result.AllOf?.Clear();
        var meaningfulSchemas = schema.AllOf
                                    .Where(x => x is not null && (schemasToExclude is null || !schemasToExclude.Contains(x)))
                                    .Where(x => (x.IsSemanticallyMeaningful() || x.AllOf is { Count: > 0 }) && (filter == null || filter(x)))
                                    .Select(x => MergeIntersectionSchemaEntries(x, schemasToExclude, overrideIntersection, filter))
                                    .OfType<IOpenApiSchema>()
                                    .ToArray();
        var entriesToMerge = meaningfulSchemas.FlattenEmptyEntries(static x => x.AllOf).Union(meaningfulSchemas).ToArray();
        if (entriesToMerge.Select(static x => x.Discriminator).OfType<OpenApiDiscriminator>().FirstOrDefault() is OpenApiDiscriminator discriminator)
            if (result.Discriminator is null)
                result.Discriminator = discriminator;
            else
            {
                if (string.IsNullOrEmpty(result.Discriminator.PropertyName) && !string.IsNullOrEmpty(discriminator.PropertyName))
                    result.Discriminator.PropertyName = discriminator.PropertyName;
                if (discriminator.Mapping is { Count: > 0 })
                    result.Discriminator.Mapping = discriminator.Mapping.ToDictionary(static x => x.Key, static x => x.Value);
            }

        schema.AddOriginalReferenceIdExtension(result);

        result.TryAddProperties(entriesToMerge.Where(static x => x.Properties is not null).SelectMany(static x => x.Properties!));

        return result;
    }

    /// <summary>
    /// Adds a temporary extension to the schema to store the original reference id of the schema being merged.
    /// This is used to keep track of the original reference id of the schema being merged when the schema is a reference.
    /// The reference id is used to generate the class name of the schema.
    /// </summary>
    /// <param name="schema">Original schema that was merged.</param>
    /// <param name="result">Resulting merged schema.</param>
    private static void AddOriginalReferenceIdExtension(this IOpenApiSchema schema, OpenApiSchema result)
    {
        if (schema is not OpenApiSchemaReference schemaReference || string.IsNullOrEmpty(schemaReference.Reference.Id)) return;
        result.Extensions ??= new Dictionary<string, IOpenApiExtension>(StringComparer.Ordinal);
        result.Extensions.TryAdd(OpenApiKiotaMergedExtension.Name, new OpenApiKiotaMergedExtension(schemaReference.Reference.Id));
    }

    internal static string? GetMergedSchemaOriginalReferenceId(this IOpenApiSchema schema)
    {
        return schema.Extensions is not null && schema.Extensions.TryGetValue(OpenApiKiotaMergedExtension.Name, out var extension) && extension is OpenApiKiotaMergedExtension mergedExtension ?
            mergedExtension.OriginalName :
            null;
    }

    internal static void TryAddProperties(this OpenApiSchema schema, IEnumerable<KeyValuePair<string, IOpenApiSchema>> properties)
    {
        schema.Properties ??= new Dictionary<string, IOpenApiSchema>(StringComparer.Ordinal);
        foreach (var property in properties)
        {
            schema.Properties.TryAdd(property.Key, property.Value);
        }
    }

    public static bool IsIntersection(this IOpenApiSchema? schema)
    {
        var meaningfulSchemas = schema?.AllOf?.Where(static x => x.IsSemanticallyMeaningful()).ToArray();
        return meaningfulSchemas?.Count(static x => !string.IsNullOrEmpty(x.GetReferenceId())) > 1 || meaningfulSchemas?.Count(static x => string.IsNullOrEmpty(x.GetReferenceId())) > 1;
    }

    public static bool IsExclusiveUnion(this IOpenApiSchema? schema, uint exclusiveMinimumNumberOfEntries = 1)
    {
        if (schema is null) return false;
        return schema.OneOf?.Count(static x => IsSemanticallyMeaningful(x, true)) > exclusiveMinimumNumberOfEntries ||
            schema.IsArrayOfTypes();
        // so we don't consider one of object/nullable as a union type
    }
    public static bool IsArrayOfTypes(this IOpenApiSchema? schema)
    {
        if (schema is null) return false;
        return schema.Type.HasValue && !IsPowerOfTwo((uint)(schema.Type.Value & ~JsonSchemaType.Null));
    }
    private static bool IsPowerOfTwo(uint x)
    {
        return (x & (x - 1)) == 0;
    }
    private static readonly HashSet<JsonSchemaType> oDataTypes = [
        JsonSchemaType.Number,
        JsonSchemaType.Integer,
        JsonSchemaType.Number | JsonSchemaType.Null,
        JsonSchemaType.Integer | JsonSchemaType.Null,
    ];
    private static readonly Func<IOpenApiSchema, bool> isODataType = static x => x.Type is not null && oDataTypes.Contains(x.Type.Value);
    private static readonly Func<IOpenApiSchema, bool> isStringType = static x => x is { Type: JsonSchemaType.String or (JsonSchemaType.String | JsonSchemaType.Null) };
    private static bool IsODataPrimitiveTypeBackwardCompatible(this IOpenApiSchema schema)
    {
        return schema.IsExclusiveUnion() &&
                schema.OneOf is { Count: 3 } &&
                schema.OneOf.Count(static x => x.Enum is { Count: > 0 }) == 1 &&
                schema.OneOf.Count(isODataType) == 1 &&
                schema.OneOf.Count(isStringType) == 1
                ||
            schema.IsInclusiveUnion() &&
                schema.AnyOf is { Count: 3 } &&
                schema.AnyOf.Count(static x => x.Enum is { Count: > 0 }) == 1 &&
                schema.AnyOf.Count(isODataType) == 1 &&
                schema.AnyOf.Count(isStringType) == 1;
    }
    public static bool IsODataPrimitiveType(this IOpenApiSchema schema)
    {
        if (schema is null) return false;
        return schema.IsExclusiveUnion() &&
               schema.OneOf is { Count: 3 } &&
               schema.OneOf.Count(static x => isStringType(x) && (x.Enum is { Count: > 0 })) == 1 &&
               schema.OneOf.Count(isODataType) == 1 &&
               schema.OneOf.Count(isStringType) == 2
               ||
               schema.IsInclusiveUnion() &&
               schema.AnyOf is { Count: 3 } &&
               schema.AnyOf.Count(static x => isStringType(x) && (x.Enum is { Count: > 0 })) == 1 &&
               schema.AnyOf.Count(isODataType) == 1 &&
               schema.AnyOf.Count(isStringType) == 2
               ||
               schema.IsODataPrimitiveTypeBackwardCompatible();
    }
    public static bool IsEnum(this IOpenApiSchema? schema)
    {
        if (schema is null || schema.Enum is null) return false;
        return schema.Enum.Any(static x => x.GetValueKind() is JsonValueKind.String &&
                                    x.GetValue<string>() is string value &&
                                    !string.IsNullOrEmpty(value)); // number and boolean enums are not supported
    }
    public static bool IsComposedEnum(this IOpenApiSchema? schema)
    {
        if (schema is null) return false;
        return schema.AnyOf?.Count(static x => !x.IsSemanticallyMeaningful(true)) == 1 && schema.AnyOf.Count(static x => x.IsEnum()) == 1 ||
                schema.OneOf?.Count(static x => !x.IsSemanticallyMeaningful(true)) == 1 && schema.OneOf.Count(static x => x.IsEnum()) == 1;
    }
    public static bool IsSemanticallyMeaningful(this IOpenApiSchema schema, bool ignoreNullableObjects = false, bool ignoreEnums = false, bool ignoreArrays = false, bool ignoreType = false)
    {
        if (schema is null) return false;
        return schema.HasAnyProperty() ||
                (!ignoreEnums && schema.Enum is { Count: > 0 }) ||
                (!ignoreArrays && schema.Items != null) ||
                (!ignoreType && schema.Type is not null &&
                    ((ignoreNullableObjects && !schema.IsObjectType()) ||
                    !ignoreNullableObjects)) ||
                !string.IsNullOrEmpty(schema.Format) ||
                !string.IsNullOrEmpty(schema.GetReferenceId());
    }
    public static IEnumerable<string> GetSchemaReferenceIds(this IOpenApiSchema schema, HashSet<IOpenApiSchema>? visitedSchemas = null)
    {
        visitedSchemas ??= new();
        if (schema != null && !visitedSchemas.Contains(schema))
        {
            visitedSchemas.Add(schema);
            var result = new List<string>();
            if (schema.GetReferenceId() is string refId && !string.IsNullOrEmpty(refId))
                result.Add(refId);
            if (schema.Items != null)
            {
                if (schema.Items.GetReferenceId() is string itemsRefId && !string.IsNullOrEmpty(itemsRefId))
                    result.Add(itemsRefId);
                result.AddRange(schema.Items.GetSchemaReferenceIds(visitedSchemas));
            }
            var subSchemaReferences = (schema.Properties?.Values ?? Enumerable.Empty<IOpenApiSchema>())
                                        .Union(schema.AnyOf ?? Enumerable.Empty<IOpenApiSchema>())
                                        .Union(schema.AllOf ?? Enumerable.Empty<IOpenApiSchema>())
                                        .Union(schema.OneOf ?? Enumerable.Empty<IOpenApiSchema>())
                                        .SelectMany(x => x.GetSchemaReferenceIds(visitedSchemas))
                                        .ToList();// this to list is important otherwise the any marks the schemas as visited and add range doesn't find anything
            if (subSchemaReferences.Count != 0)
                result.AddRange(subSchemaReferences);
            return result.Distinct();
        }

        return [];
    }
    private static IEnumerable<IOpenApiSchema> FlattenEmptyEntries(this IEnumerable<IOpenApiSchema> schemas, Func<IOpenApiSchema, IList<IOpenApiSchema>?> subsequentGetter, int? maxDepth = default)
    {
        if (schemas == null) return [];
        ArgumentNullException.ThrowIfNull(subsequentGetter);

        if ((maxDepth ?? 1) <= 0)
            return schemas;

        var result = schemas.ToList();
        var permutations = new Dictionary<IOpenApiSchema, IEnumerable<IOpenApiSchema>>();
        foreach (var item in result)
        {
            var subsequentItems = subsequentGetter(item);
            if (subsequentItems is { Count: > 0 })
                permutations.Add(item, subsequentItems.FlattenEmptyEntries(subsequentGetter, maxDepth.HasValue ? --maxDepth : default));
        }
        if (permutations.Count > 0)
        {
            foreach (var permutation in permutations)
            {
                var index = result.IndexOf(permutation.Key);
                result.RemoveAt(index);
                var offset = 0;
                foreach (var insertee in permutation.Value)
                {
                    result.Insert(index + offset, insertee);
                    offset++;
                }
            }
        }
        return result;
    }
    internal static string GetDiscriminatorPropertyName(this IOpenApiSchema schema, HashSet<IOpenApiSchema>? visitedSchemas = default)
    {

        if (schema == null)
            return string.Empty;

        visitedSchemas ??= [];
        if (visitedSchemas.Contains(schema))
            return string.Empty;
        visitedSchemas.Add(schema);

        if (!string.IsNullOrEmpty(schema.Discriminator?.PropertyName))
            return schema.Discriminator.PropertyName;

        if (schema.OneOf?.Select(x => x.GetDiscriminatorPropertyName(visitedSchemas)).FirstOrDefault(static x => !string.IsNullOrEmpty(x)) is string oneOfDiscriminatorPropertyName)
            return oneOfDiscriminatorPropertyName;
        if (schema.AnyOf?.Select(x => x.GetDiscriminatorPropertyName(visitedSchemas)).FirstOrDefault(static x => !string.IsNullOrEmpty(x)) is string anyOfDiscriminatorPropertyName)
            return anyOfDiscriminatorPropertyName;
        if (schema.AllOf?.Select(x => x.GetDiscriminatorPropertyName(visitedSchemas)).FirstOrDefault(static x => !string.IsNullOrEmpty(x)) is string allOfDiscriminatorPropertyName)
            return allOfDiscriminatorPropertyName;

        return string.Empty;
    }
    internal static IEnumerable<KeyValuePair<string, string>> GetDiscriminatorMappings(this IOpenApiSchema schema, ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> inheritanceIndex)
        => GetDiscriminatorMappings(schema, inheritanceIndex, false);
    private static IEnumerable<KeyValuePair<string, string>> GetDiscriminatorMappings(IOpenApiSchema schema, ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> inheritanceIndex, bool lookupKeyInParentMapping)
    {
        if (schema == null)
            return [];
        if (schema.Discriminator?.Mapping is not { Count: > 0 })
        {
            if (schema.OneOf is { Count: > 0 })
                // Pass lookupKeyInParentMapping: true so each member looks up its key in the parent's mapping
                return schema.OneOf.SelectMany(x => GetDiscriminatorMappings(x, inheritanceIndex, lookupKeyInParentMapping: true));
            if (schema.AnyOf is { Count: > 0 })
                // Pass lookupKeyInParentMapping: true so each member looks up its key in the parent's mapping
                return schema.AnyOf.SelectMany(x => GetDiscriminatorMappings(x, inheritanceIndex, lookupKeyInParentMapping: true));
            if (schema.IsInherited())
            {
                // ensure we're in an inheritance context and get the discriminator from the parent when available
                // First check inline schemas
                if (schema.AllOf?.OfType<OpenApiSchema>().FirstOrDefault(allOfEvaluatorForMappings) is { } allOfEntry)
                    return GetDiscriminatorMappings(allOfEntry, inheritanceIndex, lookupKeyInParentMapping);
                // When looking up the key for a specific schema in its parent's mapping (e.g. a oneOf member),
                // resolve $ref allOf entries and return only the entry that maps to the current schema.
                // This avoids O(n²) expansion when a base type has a large discriminator mapping.
                // For regular inheritance (lookupKeyInParentMapping=false), fall through to the
                // inheritance-index path which returns subtypes without the O(n²) expansion.
                if (lookupKeyInParentMapping
                    && schema.AllOf?.OfType<OpenApiSchemaReference>()
                            .Select(static x => x.RecursiveTarget)
                            .OfType<OpenApiSchema>()
                            .FirstOrDefault(allOfEvaluatorForMappings) is { } allOfRefTarget
                    && schema.GetReferenceId() is string currentRefId
                    && !string.IsNullOrEmpty(currentRefId))
                {
                    var filteredMappings = (allOfRefTarget.Discriminator?.Mapping ?? new Dictionary<string, OpenApiSchemaReference>())
                        .Where(x => string.Equals(x.Value.Reference.Id, currentRefId, StringComparison.OrdinalIgnoreCase))
                        .Select(static x => KeyValuePair.Create(x.Key, x.Value.Reference.Id!))
                        .ToList();
                    if (filteredMappings.Count > 0)
                        return filteredMappings;
                }
            }
            if (schema.GetReferenceId() is string refId && !string.IsNullOrEmpty(refId))
                return GetAllInheritanceSchemaReferences(refId, inheritanceIndex)
                           .Where(static x => !string.IsNullOrEmpty(x))
                           .Select(x => KeyValuePair.Create(x, x))
                           .Union([KeyValuePair.Create(refId, refId)]);
            return [];
        }

        return schema.Discriminator
                .Mapping
                .Where(static x => !string.IsNullOrEmpty(x.Value.Reference.Id))
                .Select(static x => KeyValuePair.Create(x.Key, x.Value.Reference.Id!));
    }
    private static readonly Func<IOpenApiSchema, bool> allOfEvaluatorForMappings = static x => x.Discriminator?.Mapping is { Count: > 0 };
    private static IEnumerable<string> GetAllInheritanceSchemaReferences(string currentReferenceId, ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> inheritanceIndex)
    {
        ArgumentException.ThrowIfNullOrEmpty(currentReferenceId);
        ArgumentNullException.ThrowIfNull(inheritanceIndex);
        if (inheritanceIndex.TryGetValue(currentReferenceId, out var dependents))
            return dependents.Keys.Union(dependents.Keys.SelectMany(x => GetAllInheritanceSchemaReferences(x, inheritanceIndex))).Distinct(StringComparer.OrdinalIgnoreCase);
        return [];
    }
    internal static string GetClassName(this IOpenApiSchema? schema)
    {
        if (schema?.GetReferenceId() is string referenceId && !string.IsNullOrEmpty(referenceId))
            return referenceId[(referenceId.LastIndexOf('.') + 1)..];
        return string.Empty;
    }
}

