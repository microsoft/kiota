using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Extensions;
public static class OpenApiSchemaExtensions
{
    private static readonly Func<OpenApiSchema, IList<OpenApiSchema>> classNamesFlattener = x =>
    (x.AnyOf ?? Enumerable.Empty<OpenApiSchema>()).Union(x.AllOf).Union(x.OneOf).ToList();
    public static IEnumerable<string> GetSchemaNames(this OpenApiSchema schema, bool directOnly = false)
    {
        if (schema == null)
            return [];
        if (!directOnly && schema.Items != null)
            return schema.Items.GetSchemaNames();
        if (!string.IsNullOrEmpty(schema.Reference?.Id))
            return [schema.Reference.Id.Split('/')[^1].Split('.')[^1]];
        if (!directOnly && schema.AnyOf.Any())
            return schema.AnyOf.FlattenIfRequired(classNamesFlattener);
        if (!directOnly && schema.AllOf.Any())
            return schema.AllOf.FlattenIfRequired(classNamesFlattener);
        if (!directOnly && schema.OneOf.Any())
            return schema.OneOf.FlattenIfRequired(classNamesFlattener);
        return [];
    }
    internal static IEnumerable<OpenApiSchema> FlattenSchemaIfRequired(this IList<OpenApiSchema> schemas, Func<OpenApiSchema, IList<OpenApiSchema>> subsequentGetter)
    {
        if (schemas is null) return [];
        return schemas.Count == 1 && !schemas[0].HasAnyProperty() && string.IsNullOrEmpty(schemas[0].Reference?.Id) ?
                    schemas.FlattenEmptyEntries(subsequentGetter, 1) :
                    schemas;
    }
    private static IEnumerable<string> FlattenIfRequired(this IList<OpenApiSchema> schemas, Func<OpenApiSchema, IList<OpenApiSchema>> subsequentGetter)
    {
        return schemas.FlattenSchemaIfRequired(subsequentGetter).SelectMany(static x => x.GetSchemaNames());
    }

    public static string GetSchemaName(this OpenApiSchema schema, bool directOnly = false)
    {
        return schema.GetSchemaNames(directOnly).LastOrDefault()?.TrimStart('$') ?? string.Empty;// OData $ref
    }

    public static bool IsReferencedSchema(this OpenApiSchema schema)
    {
        var isReference = schema?.Reference != null;
        if (isReference && schema!.Reference.IsExternal)
            throw new NotSupportedException("External references are not supported in this version of Kiota. While Kiota awaits on OpenAPI.Net to support inlining external references, you can use https://www.nuget.org/packages/Microsoft.OpenApi.Hidi to generate an OpenAPI description with inlined external references and then use this new reference with Kiota.");
        return isReference;
    }

    public static bool IsArray(this OpenApiSchema? schema)
    {
        return schema is { Type: JsonSchemaType.Array or (JsonSchemaType.Array | JsonSchemaType.Null) } && schema.Items is not null &&
            (schema.Items.IsComposedEnum() ||
            schema.Items.IsEnum() ||
            schema.Items.IsSemanticallyMeaningful() ||
            FlattenEmptyEntries([schema.Items], static x => x.AnyOf.Union(x.AllOf).Union(x.OneOf).ToList(), 1).FirstOrDefault() is OpenApiSchema flat && flat.IsSemanticallyMeaningful());
    }

    public static bool IsObjectType(this OpenApiSchema? schema)
    {
        return schema is { Type: JsonSchemaType.Object or (JsonSchemaType.Object | JsonSchemaType.Null) };
    }
    public static bool HasAnyProperty(this OpenApiSchema? schema)
    {
        return schema?.Properties is { Count: > 0 };
    }
    public static bool IsInclusiveUnion(this OpenApiSchema? schema, uint exclusiveMinimumNumberOfEntries = 1)
    {
        return schema?.AnyOf?.Count(static x => IsSemanticallyMeaningful(x, true)) > exclusiveMinimumNumberOfEntries;
        // so we don't consider any of object/nullable as a union type
    }

    public static bool IsInherited(this OpenApiSchema? schema)
    {
        if (schema is null) return false;
        var meaningfulMemberSchemas = schema.AllOf.FlattenSchemaIfRequired(static x => x.AllOf)
                                                                .Where(static x => x.IsSemanticallyMeaningful(ignoreEnums: true, ignoreArrays: true, ignoreType: true))
                                                                // the next line ensures the meaningful schema are objects as it won't make sense inheriting from a primitive despite it being meaningful.
                                                                .Where(static x => string.IsNullOrEmpty(x.Reference?.Id) || x.Type is null || !x.Type.HasValue || (x.Type.Value & JsonSchemaType.Object) is JsonSchemaType.Object)
                                                                .ToArray();
        var isRootSchemaMeaningful = schema.IsSemanticallyMeaningful(ignoreEnums: true, ignoreArrays: true, ignoreType: true);
        return meaningfulMemberSchemas.Count(static x => !string.IsNullOrEmpty(x.Reference?.Id)) == 1 &&
            (meaningfulMemberSchemas.Count(static x => string.IsNullOrEmpty(x.Reference?.Id)) == 1 ||
            isRootSchemaMeaningful);
    }

    internal static OpenApiSchema? MergeAllOfSchemaEntries(this OpenApiSchema? schema, HashSet<OpenApiSchema>? schemasToExclude = default, Func<OpenApiSchema, bool>? filter = default)
    {
        return schema.MergeIntersectionSchemaEntries(schemasToExclude, true, filter);
    }

    internal static OpenApiSchema? MergeInclusiveUnionSchemaEntries(this OpenApiSchema? schema)
    {
        if (schema is null || !schema.IsInclusiveUnion(0)) return null;
        var result = new OpenApiSchema(schema);
        result.AnyOf.Clear();
        result.TryAddProperties(schema.AnyOf.SelectMany(static x => x.Properties));
        return result;
    }

    internal static OpenApiSchema? MergeExclusiveUnionSchemaEntries(this OpenApiSchema? schema)
    {
        if (schema is null || !schema.IsExclusiveUnion(0)) return null;
        var result = new OpenApiSchema(schema);
        result.OneOf.Clear();
        result.TryAddProperties(schema.OneOf.SelectMany(static x => x.Properties));
        return result;
    }

    internal static OpenApiSchema? MergeSingleInclusiveUnionInheritanceOrIntersectionSchemaEntries(this OpenApiSchema? schema)
    {
        if (schema is not null
            && schema.IsInclusiveUnion(0)
            && schema.AnyOf.OnlyOneOrDefault() is OpenApiSchema subSchema
            && (subSchema.IsInherited() || subSchema.IsIntersection()))
        {
            var result = new OpenApiSchema(schema);
            result.AnyOf.Clear();
            result.TryAddProperties(subSchema.Properties);
            result.AllOf.AddRange(subSchema.AllOf);
            return result;
        }

        return null;
    }

    internal static OpenApiSchema? MergeSingleExclusiveUnionInheritanceOrIntersectionSchemaEntries(this OpenApiSchema? schema)
    {
        if (schema is not null
            && schema.IsExclusiveUnion(0)
            && schema.OneOf.OnlyOneOrDefault() is OpenApiSchema subSchema
            && (subSchema.IsInherited() || subSchema.IsIntersection()))
        {
            var result = new OpenApiSchema(schema);
            result.OneOf.Clear();
            result.TryAddProperties(subSchema.Properties);
            result.AllOf.AddRange(subSchema.AllOf);
            return result;
        }

        return null;
    }

    internal static OpenApiSchema? MergeIntersectionSchemaEntries(this OpenApiSchema? schema, HashSet<OpenApiSchema>? schemasToExclude = default, bool overrideIntersection = false, Func<OpenApiSchema, bool>? filter = default)
    {
        if (schema is null) return null;
        if (!schema.IsIntersection() && !overrideIntersection) return schema;
        var result = new OpenApiSchema(schema);
        result.AllOf.Clear();
        var meaningfulSchemas = schema.AllOf
                                    .Where(x => (x.IsSemanticallyMeaningful() || x.AllOf.Any()) && (filter == null || filter(x)))
                                    .Select(x => MergeIntersectionSchemaEntries(x, schemasToExclude, overrideIntersection, filter))
                                    .Where(x => x is not null && (schemasToExclude is null || !schemasToExclude.Contains(x)))
                                    .OfType<OpenApiSchema>()
                                    .ToArray();
        var entriesToMerge = meaningfulSchemas.FlattenEmptyEntries(static x => x.AllOf).Union(meaningfulSchemas).ToArray();
        if (entriesToMerge.Select(static x => x.Discriminator).OfType<OpenApiDiscriminator>().FirstOrDefault() is OpenApiDiscriminator discriminator)
            if (result.Discriminator is null)
                result.Discriminator = discriminator;
            else if (string.IsNullOrEmpty(result.Discriminator.PropertyName) && !string.IsNullOrEmpty(discriminator.PropertyName))
                result.Discriminator.PropertyName = discriminator.PropertyName;
            else if (discriminator.Mapping?.Any() ?? false)
                result.Discriminator.Mapping = discriminator.Mapping.ToDictionary(static x => x.Key, static x => x.Value);

        result.TryAddProperties(entriesToMerge.SelectMany(static x => x.Properties));

        return result;
    }

    internal static void TryAddProperties(this OpenApiSchema schema, IEnumerable<KeyValuePair<string, OpenApiSchema>> properties)
    {
        foreach (var property in properties)
        {
            schema.Properties.TryAdd(property.Key, property.Value);
        }
    }

    public static bool IsIntersection(this OpenApiSchema? schema)
    {
        var meaningfulSchemas = schema?.AllOf?.Where(static x => x.IsSemanticallyMeaningful()).ToArray();
        return meaningfulSchemas?.Count(static x => !string.IsNullOrEmpty(x.Reference?.Id)) > 1 || meaningfulSchemas?.Count(static x => string.IsNullOrEmpty(x.Reference?.Id)) > 1;
    }

    public static bool IsExclusiveUnion(this OpenApiSchema? schema, uint exclusiveMinimumNumberOfEntries = 1)
    {
        return schema?.OneOf?.Count(static x => IsSemanticallyMeaningful(x, true)) > exclusiveMinimumNumberOfEntries;
        // so we don't consider one of object/nullable as a union type
    }
    private static readonly HashSet<JsonSchemaType> oDataTypes = [
        JsonSchemaType.Number,
        JsonSchemaType.Integer,
    ];
    private static readonly Func<OpenApiSchema, bool> isODataType = static x => x.Type is not null && oDataTypes.Contains(x.Type.Value);
    private static readonly Func<OpenApiSchema, bool> isStringType = static x => x is { Type: JsonSchemaType.String or (JsonSchemaType.String | JsonSchemaType.Null) };
    private static bool IsODataPrimitiveTypeBackwardCompatible(this OpenApiSchema schema)
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
    public static bool IsODataPrimitiveType(this OpenApiSchema schema)
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
    public static bool IsEnum(this OpenApiSchema schema)
    {
        if (schema is null) return false;
        return schema.Enum.Any(static x => x.GetValueKind() is JsonValueKind.String &&
                                    x.GetValue<string>() is string value &&
                                    !string.IsNullOrEmpty(value)); // number and boolean enums are not supported
    }
    public static bool IsComposedEnum(this OpenApiSchema schema)
    {
        if (schema is null) return false;
        return schema.AnyOf.Count(static x => !x.IsSemanticallyMeaningful(true)) == 1 && schema.AnyOf.Count(static x => x.IsEnum()) == 1 ||
                schema.OneOf.Count(static x => !x.IsSemanticallyMeaningful(true)) == 1 && schema.OneOf.Count(static x => x.IsEnum()) == 1;
    }
    public static bool IsSemanticallyMeaningful(this OpenApiSchema schema, bool ignoreNullableObjects = false, bool ignoreEnums = false, bool ignoreArrays = false, bool ignoreType = false)
    {
        if (schema is null) return false;
        return schema.HasAnyProperty() ||
                (!ignoreEnums && schema.Enum is { Count: > 0 }) ||
                (!ignoreArrays && schema.Items != null) ||
                (!ignoreType && schema.Type is not null &&
                    ((ignoreNullableObjects && !schema.IsObjectType()) ||
                    !ignoreNullableObjects)) ||
                !string.IsNullOrEmpty(schema.Format) ||
                !string.IsNullOrEmpty(schema.Reference?.Id);
    }
    public static IEnumerable<string> GetSchemaReferenceIds(this OpenApiSchema schema, HashSet<OpenApiSchema>? visitedSchemas = null)
    {
        visitedSchemas ??= new();
        if (schema != null && !visitedSchemas.Contains(schema))
        {
            visitedSchemas.Add(schema);
            var result = new List<string>();
            if (!string.IsNullOrEmpty(schema.Reference?.Id))
                result.Add(schema.Reference.Id);
            if (schema.Items != null)
            {
                if (!string.IsNullOrEmpty(schema.Items.Reference?.Id))
                    result.Add(schema.Items.Reference.Id);
                result.AddRange(schema.Items.GetSchemaReferenceIds(visitedSchemas));
            }
            var subSchemaReferences = (schema.Properties?.Values ?? Enumerable.Empty<OpenApiSchema>())
                                        .Union(schema.AnyOf ?? Enumerable.Empty<OpenApiSchema>())
                                        .Union(schema.AllOf ?? Enumerable.Empty<OpenApiSchema>())
                                        .Union(schema.OneOf ?? Enumerable.Empty<OpenApiSchema>())
                                        .SelectMany(x => x.GetSchemaReferenceIds(visitedSchemas))
                                        .ToList();// this to list is important otherwise the any marks the schemas as visited and add range doesn't find anything
            if (subSchemaReferences.Count != 0)
                result.AddRange(subSchemaReferences);
            return result.Distinct();
        }

        return [];
    }
    private static IEnumerable<OpenApiSchema> FlattenEmptyEntries(this IEnumerable<OpenApiSchema> schemas, Func<OpenApiSchema, IList<OpenApiSchema>> subsequentGetter, int? maxDepth = default)
    {
        if (schemas == null) return [];
        ArgumentNullException.ThrowIfNull(subsequentGetter);

        if ((maxDepth ?? 1) <= 0)
            return schemas;

        var result = schemas.ToList();
        var permutations = new Dictionary<OpenApiSchema, IEnumerable<OpenApiSchema>>();
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
    internal static string GetDiscriminatorPropertyName(this OpenApiSchema schema, HashSet<OpenApiSchema>? visitedSchemas = default)
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
    internal static IEnumerable<KeyValuePair<string, string>> GetDiscriminatorMappings(this OpenApiSchema schema, ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> inheritanceIndex)
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
            else if (!string.IsNullOrEmpty(schema.Reference?.Id))
                return GetAllInheritanceSchemaReferences(schema.Reference.Id, inheritanceIndex)
                           .Where(static x => !string.IsNullOrEmpty(x))
                           .Select(x => KeyValuePair.Create(x, x))
                           .Union([KeyValuePair.Create(schema.Reference.Id, schema.Reference.Id)]);
            else
                return [];

        return schema.Discriminator
                .Mapping;
    }
    private static readonly Func<OpenApiSchema, bool> allOfEvaluatorForMappings = static x => x.Discriminator?.Mapping.Any() ?? false;
    private static IEnumerable<string> GetAllInheritanceSchemaReferences(string currentReferenceId, ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> inheritanceIndex)
    {
        ArgumentException.ThrowIfNullOrEmpty(currentReferenceId);
        ArgumentNullException.ThrowIfNull(inheritanceIndex);
        if (inheritanceIndex.TryGetValue(currentReferenceId, out var dependents))
            return dependents.Keys.Union(dependents.Keys.SelectMany(x => GetAllInheritanceSchemaReferences(x, inheritanceIndex))).Distinct(StringComparer.OrdinalIgnoreCase);
        return [];
    }
}

