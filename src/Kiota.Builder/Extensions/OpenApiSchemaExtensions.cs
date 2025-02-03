using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.Interfaces;
using Microsoft.OpenApi.Models.References;

namespace Kiota.Builder.Extensions;
public static class OpenApiSchemaExtensions
{
    private static readonly Func<IOpenApiSchema, IList<IOpenApiSchema>> classNamesFlattener = x =>
    (x.AnyOf ?? Enumerable.Empty<IOpenApiSchema>()).Union(x.AllOf).Union(x.OneOf).ToList();
    public static IEnumerable<string> GetSchemaNames(this IOpenApiSchema schema, bool directOnly = false)
    {
        if (schema == null)
            return [];
        if (!directOnly && schema.Items != null)
            return schema.Items.GetSchemaNames();
        if (schema.GetReferenceId() is string refId && !string.IsNullOrEmpty(refId))
            return [refId.Split('/')[^1].Split('.')[^1]];
        if (!directOnly && schema.AnyOf.Any())
            return schema.AnyOf.FlattenIfRequired(classNamesFlattener);
        if (!directOnly && schema.AllOf.Any())
            return schema.AllOf.FlattenIfRequired(classNamesFlattener);
        if (!directOnly && schema.OneOf.Any())
            return schema.OneOf.FlattenIfRequired(classNamesFlattener);
        return [];
    }
    internal static string? GetReferenceId(this IOpenApiSchema schema)
    {
        return schema switch
        {
            OpenApiSchemaReference reference => reference.Reference?.Id,
            _ => null,
        };
    }
    internal static IEnumerable<IOpenApiSchema> FlattenSchemaIfRequired(this IList<IOpenApiSchema> schemas, Func<IOpenApiSchema, IList<IOpenApiSchema>> subsequentGetter)
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
            OpenApiSchemaReference reference => reference.Reference.IsExternal ?
                    // TODO remove this failsafe once we have a test with external documents.
                    throw new NotSupportedException("External references are not supported in this version of Kiota. While Kiota awaits on OpenAPI.Net to support inlining external references, you can use https://www.nuget.org/packages/Microsoft.OpenApi.Hidi to generate an OpenAPI description with inlined external references and then use this new reference with Kiota.") :
                    true,
            _ => false,
        };
    }

    public static bool IsArray(this IOpenApiSchema? schema)
    {
        return schema is { Type: JsonSchemaType.Array or (JsonSchemaType.Array | JsonSchemaType.Null) } && schema.Items is not null &&
            (schema.Items.IsComposedEnum() ||
            schema.Items.IsEnum() ||
            schema.Items.IsSemanticallyMeaningful() ||
            FlattenEmptyEntries([schema.Items], static x => x.AnyOf.Union(x.AllOf).Union(x.OneOf).ToList(), 1).FirstOrDefault() is OpenApiSchema flat && flat.IsSemanticallyMeaningful());
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
        var result = schema.CreateShallowCopy();
        result.AnyOf.Clear();
        result.TryAddProperties(schema.AnyOf.SelectMany(static x => x.Properties));
        return result;
    }

    internal static IOpenApiSchema? MergeExclusiveUnionSchemaEntries(this IOpenApiSchema? schema)
    {
        if (schema is null || !schema.IsExclusiveUnion(0)) return null;
        var result = schema.CreateShallowCopy();
        result.OneOf.Clear();
        result.TryAddProperties(schema.OneOf.SelectMany(static x => x.Properties));
        return result;
    }

    internal static IOpenApiSchema? MergeSingleInclusiveUnionInheritanceOrIntersectionSchemaEntries(this IOpenApiSchema? schema)
    {
        if (schema is not null
            && schema.IsInclusiveUnion(0)
            && schema.AnyOf.OnlyOneOrDefault() is OpenApiSchema subSchema
            && (subSchema.IsInherited() || subSchema.IsIntersection()))
        {
            var result = schema.CreateShallowCopy();
            result.AnyOf.Clear();
            result.TryAddProperties(subSchema.Properties);
            result.AllOf.AddRange(subSchema.AllOf);
            return result;
        }

        return null;
    }

    internal static IOpenApiSchema? MergeSingleExclusiveUnionInheritanceOrIntersectionSchemaEntries(this IOpenApiSchema? schema)
    {
        if (schema is not null
            && schema.IsExclusiveUnion(0)
            && schema.OneOf.OnlyOneOrDefault() is OpenApiSchema subSchema
            && (subSchema.IsInherited() || subSchema.IsIntersection()))
        {
            var result = schema.CreateShallowCopy();
            result.OneOf.Clear();
            result.TryAddProperties(subSchema.Properties);
            result.AllOf.AddRange(subSchema.AllOf);
            return result;
        }

        return null;
    }

    internal static IOpenApiSchema? MergeIntersectionSchemaEntries(this IOpenApiSchema? schema, HashSet<IOpenApiSchema>? schemasToExclude = default, bool overrideIntersection = false, Func<IOpenApiSchema, bool>? filter = default)
    {
        if (schema is null) return null;
        if (!schema.IsIntersection() && !overrideIntersection) return schema;
        var result = schema.CreateShallowCopy();
        result.AllOf.Clear();
        var meaningfulSchemas = schema.AllOf
                                    .Where(x => (x.IsSemanticallyMeaningful() || x.AllOf.Any()) && (filter == null || filter(x)))
                                    .Select(x => MergeIntersectionSchemaEntries(x, schemasToExclude, overrideIntersection, filter))
                                    .Where(x => x is not null && (schemasToExclude is null || !schemasToExclude.Contains(x)))
                                    .OfType<IOpenApiSchema>()
                                    .ToArray();
        var entriesToMerge = meaningfulSchemas.FlattenEmptyEntries(static x => x.AllOf).Union(meaningfulSchemas).ToArray();
        if (entriesToMerge.Select(static x => x.Discriminator).OfType<OpenApiDiscriminator>().FirstOrDefault() is OpenApiDiscriminator discriminator &&
            result is OpenApiSchema resultSchema)
            if (resultSchema.Discriminator is null)
                resultSchema.Discriminator = discriminator;
            else if (string.IsNullOrEmpty(resultSchema.Discriminator.PropertyName) && !string.IsNullOrEmpty(discriminator.PropertyName))
                resultSchema.Discriminator.PropertyName = discriminator.PropertyName;
            else if (discriminator.Mapping?.Any() ?? false)
                resultSchema.Discriminator.Mapping = discriminator.Mapping.ToDictionary(static x => x.Key, static x => x.Value);

        result.TryAddProperties(entriesToMerge.SelectMany(static x => x.Properties));

        return result;
    }

    internal static void TryAddProperties(this IOpenApiSchema schema, IEnumerable<KeyValuePair<string, IOpenApiSchema>> properties)
    {
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
        return schema?.OneOf?.Count(static x => IsSemanticallyMeaningful(x, true)) > exclusiveMinimumNumberOfEntries;
        // so we don't consider one of object/nullable as a union type
    }
    private static readonly HashSet<JsonSchemaType> oDataTypes = [
        JsonSchemaType.Number,
        JsonSchemaType.Integer,
    ];
    private static readonly Func<IOpenApiSchema, bool> isODataType = static x => x.Type is not null && oDataTypes.Contains(x.Type.Value);
    private static readonly Func<IOpenApiSchema, bool> isStringType = static x => x is { Type: JsonSchemaType.String or (JsonSchemaType.String | JsonSchemaType.Null) };
    private static bool IsODataPrimitiveTypeBackwardCompatible(this IOpenApiSchema schema)
    {
        return schema.IsExclusiveUnion() &&
                schema.OneOf.Count == 3 &&
                schema.OneOf.Count(static x => x.Enum?.Any() ?? false) == 1 &&
                schema.OneOf.Count(isODataType) == 1 &&
                schema.OneOf.Count(isStringType) == 1
                ||
            schema.IsInclusiveUnion() &&
                schema.AnyOf.Count == 3 &&
                schema.AnyOf.Count(static x => x.Enum?.Any() ?? false) == 1 &&
                schema.AnyOf.Count(isODataType) == 1 &&
                schema.AnyOf.Count(isStringType) == 1;
    }
    public static bool IsODataPrimitiveType(this IOpenApiSchema schema)
    {
        return schema.IsExclusiveUnion() &&
               schema.OneOf.Count == 3 &&
               schema.OneOf.Count(static x => isStringType(x) && (x.Enum?.Any() ?? false)) == 1 &&
               schema.OneOf.Count(isODataType) == 1 &&
               schema.OneOf.Count(isStringType) == 2
               ||
               schema.IsInclusiveUnion() &&
               schema.AnyOf.Count == 3 &&
               schema.AnyOf.Count(static x => isStringType(x) && (x.Enum?.Any() ?? false)) == 1 &&
               schema.AnyOf.Count(isODataType) == 1 &&
               schema.AnyOf.Count(isStringType) == 2
               ||
               schema.IsODataPrimitiveTypeBackwardCompatible();
    }
    public static bool IsEnum(this IOpenApiSchema schema)
    {
        if (schema is null) return false;
        return schema.Enum.Any(static x => x.GetValueKind() is JsonValueKind.String &&
                                    x.GetValue<string>() is string value &&
                                    !string.IsNullOrEmpty(value)); // number and boolean enums are not supported
    }
    public static bool IsComposedEnum(this IOpenApiSchema schema)
    {
        if (schema is null) return false;
        return schema.AnyOf.Count(static x => !x.IsSemanticallyMeaningful(true)) == 1 && schema.AnyOf.Count(static x => x.IsEnum()) == 1 ||
                schema.OneOf.Count(static x => !x.IsSemanticallyMeaningful(true)) == 1 && schema.OneOf.Count(static x => x.IsEnum()) == 1;
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
    private static IEnumerable<IOpenApiSchema> FlattenEmptyEntries(this IEnumerable<IOpenApiSchema> schemas, Func<IOpenApiSchema, IList<IOpenApiSchema>> subsequentGetter, int? maxDepth = default)
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
            if (subsequentItems.Any())
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

        if (schema.OneOf.Select(x => x.GetDiscriminatorPropertyName(visitedSchemas)).FirstOrDefault(static x => !string.IsNullOrEmpty(x)) is string oneOfDiscriminatorPropertyName)
            return oneOfDiscriminatorPropertyName;
        if (schema.AnyOf.Select(x => x.GetDiscriminatorPropertyName(visitedSchemas)).FirstOrDefault(static x => !string.IsNullOrEmpty(x)) is string anyOfDiscriminatorPropertyName)
            return anyOfDiscriminatorPropertyName;
        if (schema.AllOf.Select(x => x.GetDiscriminatorPropertyName(visitedSchemas)).FirstOrDefault(static x => !string.IsNullOrEmpty(x)) is string allOfDiscriminatorPropertyName)
            return allOfDiscriminatorPropertyName;

        return string.Empty;
    }
    internal static IEnumerable<KeyValuePair<string, string>> GetDiscriminatorMappings(this IOpenApiSchema schema, ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> inheritanceIndex)
    {
        if (schema == null)
            return [];
        if (!(schema.Discriminator?.Mapping?.Any() ?? false))
            if (schema.OneOf.Any())
                return schema.OneOf.SelectMany(x => GetDiscriminatorMappings(x, inheritanceIndex));
            else if (schema.AnyOf.Any())
                return schema.AnyOf.SelectMany(x => GetDiscriminatorMappings(x, inheritanceIndex));
            else if (schema.AllOf.Any(allOfEvaluatorForMappings) && schema.AllOf[^1].Equals(schema.AllOf.Last(allOfEvaluatorForMappings)))
                // ensure the matched AllOf entry is the last in the list
                return GetDiscriminatorMappings(schema.AllOf.Last(allOfEvaluatorForMappings), inheritanceIndex);
            else if (schema.GetReferenceId() is string refId && !string.IsNullOrEmpty(refId))
                return GetAllInheritanceSchemaReferences(refId, inheritanceIndex)
                           .Where(static x => !string.IsNullOrEmpty(x))
                           .Select(x => KeyValuePair.Create(x, x))
                           .Union([KeyValuePair.Create(refId, refId)]);
            else
                return [];

        return schema.Discriminator
                .Mapping;
    }
    private static readonly Func<IOpenApiSchema, bool> allOfEvaluatorForMappings = static x => x.Discriminator?.Mapping.Any() ?? false;
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

