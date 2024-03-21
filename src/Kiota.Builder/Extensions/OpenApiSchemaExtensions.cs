using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Extensions;
public static class OpenApiSchemaExtensions
{
    private static readonly Func<OpenApiSchema, IList<OpenApiSchema>> classNamesFlattener = x =>
    (x.AnyOf ?? Enumerable.Empty<OpenApiSchema>()).Union(x.AllOf).Union(x.OneOf).ToList();
    public static IEnumerable<string> GetSchemaNames(this OpenApiSchema schema)
    {
        if (schema == null)
            return Enumerable.Empty<string>();
        if (schema.Items != null)
            return schema.Items.GetSchemaNames();
        if (!string.IsNullOrEmpty(schema.Reference?.Id))
            return new[] { schema.Reference.Id.Split('/')[^1].Split('.')[^1] };
        if (schema.AnyOf.Any())
            return schema.AnyOf.FlattenIfRequired(classNamesFlattener);
        if (schema.AllOf.Any())
            return schema.AllOf.FlattenIfRequired(classNamesFlattener);
        if (schema.OneOf.Any())
            return schema.OneOf.FlattenIfRequired(classNamesFlattener);
        if (!string.IsNullOrEmpty(schema.Title))
            return new[] { schema.Title };
        if (!string.IsNullOrEmpty(schema.Xml?.Name))
            return new[] { schema.Xml.Name };
        return Enumerable.Empty<string>();
    }
    internal static IEnumerable<OpenApiSchema> FlattenSchemaIfRequired(this IList<OpenApiSchema> schemas, Func<OpenApiSchema, IList<OpenApiSchema>> subsequentGetter)
    {
        if (schemas is null) return Enumerable.Empty<OpenApiSchema>();
        return schemas.Count == 1 ?
                    schemas.FlattenEmptyEntries(subsequentGetter, 1) :
                    schemas;
    }
    private static IEnumerable<string> FlattenIfRequired(this IList<OpenApiSchema> schemas, Func<OpenApiSchema, IList<OpenApiSchema>> subsequentGetter)
    {
        return schemas.FlattenSchemaIfRequired(subsequentGetter).Where(static x => !string.IsNullOrEmpty(x.Title)).Select(static x => x.Title);
    }

    public static string GetSchemaName(this OpenApiSchema schema)
    {
        return schema.GetSchemaNames().LastOrDefault()?.TrimStart('$') ?? string.Empty;// OData $ref
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
        return "array".Equals(schema?.Type, StringComparison.OrdinalIgnoreCase) && schema.Items != null &&
            (schema.Items.IsComposedEnum() ||
            schema.Items.IsEnum() ||
            schema.Items.IsSemanticallyMeaningful() ||
            FlattenEmptyEntries(new OpenApiSchema[] { schema.Items }, static x => x.AnyOf.Union(x.AllOf).Union(x.OneOf).ToList(), 1).FirstOrDefault() is OpenApiSchema flat && flat.IsSemanticallyMeaningful());
    }

    public static bool IsObject(this OpenApiSchema? schema)
    {
        return "object".Equals(schema?.Type, StringComparison.OrdinalIgnoreCase);
    }
    public static bool IsInclusiveUnion(this OpenApiSchema? schema)
    {
        return schema?.AnyOf?.Count(static x => IsSemanticallyMeaningful(x, true)) > 1;
        // so we don't consider any of object/nullable as a union type
    }

    public static bool IsInherited(this OpenApiSchema? schema)
    {
        if (schema is null) return false;
        var meaningfulSchemas = schema.AllOf.FlattenSchemaIfRequired(static x => x.AllOf).Where(static x => x.IsSemanticallyMeaningful()).ToArray();
        return meaningfulSchemas.Count(static x => !string.IsNullOrEmpty(x.Reference?.Id)) == 1 &&
            (meaningfulSchemas.Count(static x => string.IsNullOrEmpty(x.Reference?.Id)) == 1 ||
            schema.IsSemanticallyMeaningful());
    }

    internal static OpenApiSchema? MergeIntersectionSchemaEntries(this OpenApiSchema? schema)
    {
        if (schema is null) return null;
        if (!schema.IsIntersection()) return schema;
        var result = new OpenApiSchema(schema);
        result.AllOf.Clear();
        var meaningfulSchemas = schema.AllOf.Where(static x => x.IsSemanticallyMeaningful()).Select(MergeIntersectionSchemaEntries).Where(x => x is not null).OfType<OpenApiSchema>();
        meaningfulSchemas.SelectMany(static x => x.Properties).ToList().ForEach(x => result.Properties.TryAdd(x.Key, x.Value));
        return result;
    }

    public static bool IsIntersection(this OpenApiSchema? schema)
    {
        var meaningfulSchemas = schema?.AllOf?.Where(static x => x.IsSemanticallyMeaningful());
        return meaningfulSchemas?.Count() > 3 || meaningfulSchemas?.Count(static x => !string.IsNullOrEmpty(x.Reference?.Id)) > 1 || meaningfulSchemas?.Count(static x => string.IsNullOrEmpty(x.Reference?.Id)) > 1;
    }

    public static bool IsExclusiveUnion(this OpenApiSchema? schema)
    {
        return schema?.OneOf?.Count(static x => IsSemanticallyMeaningful(x, true)) > 1;
        // so we don't consider one of object/nullable as a union type
    }
    private static readonly HashSet<string> oDataTypes = new(StringComparer.OrdinalIgnoreCase) {
        "number",
        "integer",
    };
    public static bool IsODataPrimitiveType(this OpenApiSchema schema)
    {
        return schema.IsExclusiveUnion() &&
                schema.OneOf.Count == 3 &&
                schema.OneOf.Count(static x => x.Enum?.Any() ?? false) == 1 &&
                schema.OneOf.Count(static x => oDataTypes.Contains(x.Type)) == 1 &&
                schema.OneOf.Count(static x => "string".Equals(x.Type, StringComparison.OrdinalIgnoreCase)) == 1
                ||
            schema.IsInclusiveUnion() &&
                schema.AnyOf.Count == 3 &&
                schema.AnyOf.Count(static x => x.Enum?.Any() ?? false) == 1 &&
                schema.AnyOf.Count(static x => oDataTypes.Contains(x.Type)) == 1 &&
                schema.AnyOf.Count(static x => "string".Equals(x.Type, StringComparison.OrdinalIgnoreCase)) == 1;
    }
    public static bool IsEnum(this OpenApiSchema schema)
    {
        if (schema is null) return false;
        return schema.Enum.OfType<OpenApiString>().Any(static x => !string.IsNullOrEmpty(x.Value)) &&
                (string.IsNullOrEmpty(schema.Type) || "string".Equals(schema.Type, StringComparison.OrdinalIgnoreCase)); // number and boolean enums are not supported
    }
    public static bool IsComposedEnum(this OpenApiSchema schema)
    {
        if (schema is null) return false;
        return schema.AnyOf.Count(static x => !x.IsSemanticallyMeaningful(true)) == 1 && schema.AnyOf.Count(static x => x.IsEnum()) == 1 ||
                schema.OneOf.Count(static x => !x.IsSemanticallyMeaningful(true)) == 1 && schema.OneOf.Count(static x => x.IsEnum()) == 1;
    }
    public static bool IsSemanticallyMeaningful(this OpenApiSchema schema, bool ignoreNullableObjects = false)
    {
        if (schema is null) return false;
        return schema.Properties.Any() ||
                schema.Enum is { Count: > 0 } ||
                schema.Items != null ||
                (!string.IsNullOrEmpty(schema.Type) &&
                    ((ignoreNullableObjects && !"object".Equals(schema.Type, StringComparison.OrdinalIgnoreCase)) ||
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

        return Enumerable.Empty<string>();
    }
    private static IEnumerable<OpenApiSchema> FlattenEmptyEntries(this IEnumerable<OpenApiSchema> schemas, Func<OpenApiSchema, IList<OpenApiSchema>> subsequentGetter, int? maxDepth = default)
    {
        if (schemas == null) return Enumerable.Empty<OpenApiSchema>();
        ArgumentNullException.ThrowIfNull(subsequentGetter);

        if ((maxDepth ?? 1) <= 0)
            return schemas;

        var result = schemas.ToList();
        var permutations = new Dictionary<OpenApiSchema, IEnumerable<OpenApiSchema>>();
        foreach (var item in result)
        {
            var subsequentItems = subsequentGetter(item);
            if (string.IsNullOrEmpty(item.Title) && subsequentItems.Any())
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
    internal static string GetDiscriminatorPropertyName(this OpenApiSchema schema)
    {
        if (schema == null)
            return string.Empty;

        if (!string.IsNullOrEmpty(schema.Discriminator?.PropertyName))
            return schema.Discriminator.PropertyName;

        if (schema.OneOf.Select(GetDiscriminatorPropertyName).FirstOrDefault(static x => !string.IsNullOrEmpty(x)) is string oneOfDiscriminatorPropertyName)
            return oneOfDiscriminatorPropertyName;
        if (schema.AnyOf.Select(GetDiscriminatorPropertyName).FirstOrDefault(static x => !string.IsNullOrEmpty(x)) is string anyOfDiscriminatorPropertyName)
            return anyOfDiscriminatorPropertyName;
        if (schema.AllOf.Any())
            return GetDiscriminatorPropertyName(schema.AllOf[^1]);

        return string.Empty;
    }
    internal static IEnumerable<KeyValuePair<string, string>> GetDiscriminatorMappings(this OpenApiSchema schema, ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> inheritanceIndex)
    {
        if (schema == null)
            return Enumerable.Empty<KeyValuePair<string, string>>();
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
                           .Union(new[] { KeyValuePair.Create(schema.Reference.Id, schema.Reference.Id) });
            else
                return Enumerable.Empty<KeyValuePair<string, string>>();

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
        return Enumerable.Empty<string>();
    }
}

