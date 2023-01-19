using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Extensions;
public static class OpenApiSchemaExtensions {
    private static readonly Func<OpenApiSchema, IList<OpenApiSchema>> classNamesFlattener = x =>
    (x.AnyOf ?? Enumerable.Empty<OpenApiSchema>()).Union(x.AllOf).Union(x.OneOf).ToList();
    public static IEnumerable<string> GetSchemaNames(this OpenApiSchema schema)
    {
        if(schema == null)
            return Enumerable.Empty<string>();
        if(schema.Items != null)
            return schema.Items.GetSchemaNames();
        if(!string.IsNullOrEmpty(schema.Reference?.Id))
            return new[] {schema.Reference.Id.Split('/').Last().Split('.').Last()};
        if(schema.AnyOf.Any())
            return schema.AnyOf.FlattenIfRequired(classNamesFlattener);
        if(schema.AllOf.Any())
            return schema.AllOf.FlattenIfRequired(classNamesFlattener);
        if(schema.OneOf.Any())
            return schema.OneOf.FlattenIfRequired(classNamesFlattener);
        if(!string.IsNullOrEmpty(schema.Title))
            return new[] { schema.Title };
        if(!string.IsNullOrEmpty(schema.Xml?.Name))
            return new[] {schema.Xml.Name};
        return Enumerable.Empty<string>();
    }
    private static IEnumerable<string> FlattenIfRequired(this IList<OpenApiSchema> schemas, Func<OpenApiSchema, IList<OpenApiSchema>> subsequentGetter) {
        return (schemas.Count == 1 && string.IsNullOrEmpty(schemas.First().Title) ?
                    schemas.FlattenEmptyEntries(subsequentGetter, 1) :
                    schemas)
            .Select(static x => x.Title).Where(static x => !string.IsNullOrEmpty(x));
    }

    public static string GetSchemaName(this OpenApiSchema schema) {
        return schema.GetSchemaNames().LastOrDefault()?.TrimStart('$');// OData $ref
    }

    public static bool IsReferencedSchema(this OpenApiSchema schema) {
        var isReference = schema?.Reference != null;
        if(isReference && schema.Reference.IsExternal)
            throw new NotSupportedException("External references are not supported in this version of Kiota. While Kiota awaits on OpenAPI.Net to support inlining external references, you can use https://www.nuget.org/packages/Microsoft.OpenApi.Hidi to generate an OpenAPI description with inlined external references and then use this new reference with Kiota.");
        return isReference;
    }

    public static bool IsArray(this OpenApiSchema schema)
    {
        return (schema?.Type?.Equals("array", StringComparison.OrdinalIgnoreCase) ?? false) && schema?.Items != null;
    }

    public static bool IsObject(this OpenApiSchema schema)
    {
        return schema?.Type?.Equals("object", StringComparison.OrdinalIgnoreCase) ?? false;
    }
    public static bool IsAnyOf(this OpenApiSchema schema)
    {
        return schema?.AnyOf?.Count(IsSemanticallyMeaningful) > 1;
    }

    public static bool IsAllOf(this OpenApiSchema schema)
    {
        return schema?.AllOf?.Count(IsSemanticallyMeaningful) > 1;
    }

    public static bool IsOneOf(this OpenApiSchema schema)
    {
        return schema?.OneOf?.Count(IsSemanticallyMeaningful) > 1;
    }
    private static readonly HashSet<string> oDataTypes = new() {
        "number",
        "integer",
    };
    public static bool IsODataPrimitiveType(this OpenApiSchema schema)
    {
        return schema.IsOneOf() &&
                schema.OneOf.Count == 3 &&
                schema.OneOf.Count(static x => x.Enum?.Any() ?? false) == 1 &&
                schema.OneOf.Count(static x => oDataTypes.Contains(x.Type)) == 1 &&
                schema.OneOf.Count(static x => "string".Equals(x.Type, StringComparison.OrdinalIgnoreCase)) == 1
                ||
            schema.IsAnyOf() &&
                schema.AnyOf.Count == 3 &&
                schema.AnyOf.Count(static x => x.Enum?.Any() ?? false) == 1 &&
                schema.AnyOf.Count(static x => oDataTypes.Contains(x.Type)) == 1 &&
                schema.AnyOf.Count(static x => "string".Equals(x.Type, StringComparison.OrdinalIgnoreCase)) == 1;
    }
    public static bool IsEnum(this OpenApiSchema schema)
    {
        return schema?.Enum?.Any() ?? false;
    }
    public static bool IsComposedEnum(this OpenApiSchema schema)
    {
        return (schema.IsAnyOf() && schema.AnyOf.Any(x => x.IsEnum())) || (schema.IsOneOf() && schema.OneOf.Any(x => x.IsEnum()));
    }
    private static bool IsSemanticallyMeaningful(this OpenApiSchema schema)
    {
        return schema.Properties.Any() || schema.Items != null || !string.IsNullOrEmpty(schema.Type) || !string.IsNullOrEmpty(schema.Format) || !string.IsNullOrEmpty(schema.Reference?.Id);
    }
    public static IEnumerable<string> GetSchemaReferenceIds(this OpenApiSchema schema, HashSet<OpenApiSchema> visitedSchemas = null) {
        visitedSchemas ??= new();            
        if(schema != null && !visitedSchemas.Contains(schema)) {
            visitedSchemas.Add(schema);
            var result = new List<string>();
            if(!string.IsNullOrEmpty(schema.Reference?.Id))
                result.Add(schema.Reference.Id);
            if(schema.Items != null) {
                if(!string.IsNullOrEmpty(schema.Items.Reference?.Id))
                    result.Add(schema.Items.Reference.Id);
                result.AddRange(schema.Items.GetSchemaReferenceIds(visitedSchemas));
            }
            var subSchemaReferences = (schema.Properties?.Values ?? Enumerable.Empty<OpenApiSchema>())
                                        .Union(schema.AnyOf ?? Enumerable.Empty<OpenApiSchema>())
                                        .Union(schema.AllOf ?? Enumerable.Empty<OpenApiSchema>())
                                        .Union(schema.OneOf ?? Enumerable.Empty<OpenApiSchema>())
                                        .SelectMany(x => x.GetSchemaReferenceIds(visitedSchemas))
                                        .ToList();// this to list is important otherwise the any marks the schemas as visited and add range doesn't find anything
            if(subSchemaReferences.Any())
                result.AddRange(subSchemaReferences);
            return result.Distinct();
        }

        return Enumerable.Empty<string>();
    }
    internal static IEnumerable<OpenApiSchema> FlattenEmptyEntries(this IEnumerable<OpenApiSchema> schemas, Func<OpenApiSchema, IList<OpenApiSchema>> subsequentGetter, int? maxDepth = default) {
        if(schemas == null) return Enumerable.Empty<OpenApiSchema>();
        ArgumentNullException.ThrowIfNull(subsequentGetter);

        if((maxDepth ?? 1) <= 0)
            return schemas;

        var result = schemas.ToList();
        var permutations = new Dictionary<OpenApiSchema, IEnumerable<OpenApiSchema>>();
        foreach(var item in result)
        {
            var subsequentItems = subsequentGetter(item);
            if(string.IsNullOrEmpty(item.Title) && subsequentItems.Any())
                permutations.Add(item, subsequentItems.FlattenEmptyEntries(subsequentGetter, maxDepth.HasValue ? --maxDepth : default));
        }
        foreach(var permutation in permutations) {
            var index = result.IndexOf(permutation.Key);
            result.RemoveAt(index);
            var offset = 0;
            foreach(var insertee in permutation.Value) {
                result.Insert(index + offset, insertee);
                offset++;
            }
        }
        return result;
    }
    internal static string GetDiscriminatorPropertyName(this OpenApiSchema schema) {
        if(schema == null)
            return string.Empty;

        if (!string.IsNullOrEmpty(schema.Discriminator?.PropertyName))
            return schema.Discriminator.PropertyName;

        if(schema.OneOf.Any())
            return schema.OneOf.Select(static x => GetDiscriminatorPropertyName(x)).FirstOrDefault(static x => !string.IsNullOrEmpty(x));
        if (schema.AnyOf.Any())
            return schema.AnyOf.Select(static x => GetDiscriminatorPropertyName(x)).FirstOrDefault(static x => !string.IsNullOrEmpty(x));
        if (schema.AllOf.Any())
            return GetDiscriminatorPropertyName(schema.AllOf.Last());

        return string.Empty;
    }
    internal static IEnumerable<KeyValuePair<string, string>> GetDiscriminatorMappings(this OpenApiSchema schema, ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> inheritanceIndex) {
        if(schema == null)
            return Enumerable.Empty<KeyValuePair<string, string>>();
        if(!(schema.Discriminator?.Mapping?.Any() ?? false))
            if(schema.OneOf.Any())
                return schema.OneOf.SelectMany(x => GetDiscriminatorMappings(x, inheritanceIndex));
            else if (schema.AnyOf.Any())
                return schema.AnyOf.SelectMany(x => GetDiscriminatorMappings(x, inheritanceIndex));
            else if (schema.AllOf.Any(allOfEvaluatorForMappings) && schema.AllOf.Last().Equals(schema.AllOf.Last(allOfEvaluatorForMappings)))
                // ensure the matched AllOf entry is the last in the list
                return GetDiscriminatorMappings(schema.AllOf.Last(allOfEvaluatorForMappings), inheritanceIndex);
            else if (!string.IsNullOrEmpty(schema.Reference?.Id))
                 return GetAllInheritanceSchemaReferences(schema.Reference?.Id, inheritanceIndex)
                            .Where(static x => !string.IsNullOrEmpty(x))
                            .Select(x => KeyValuePair.Create(x, x))
                            .Union(new [] { KeyValuePair.Create(schema.Reference.Id, schema.Reference.Id) });
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

