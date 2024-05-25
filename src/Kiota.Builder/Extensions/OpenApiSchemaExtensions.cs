using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Extensions;
public static class OpenApiSchemaExtensions
{
    private static readonly Func<OpenApiSchema, IList<OpenApiSchema>> classNamesFlattener = x =>
    {
        List<OpenApiSchema> schemas = [];

        if (x.AnyOf is not null)
        {
            foreach (var schema in x.AnyOf)
            {
                schemas.Add(schema);
            }
        }

        foreach (var schema in x.AllOf)
        {
            schemas.Add(schema);
        }

        foreach (var schema in x.OneOf)
        {
            schemas.Add(schema);
        }

        return schemas;
    };

    public static IEnumerable<string> GetSchemaNames(this OpenApiSchema schema, bool directOnly = false)
    {
        if (schema == null)
            return [];

        if (!directOnly && schema.Items != null)
            return schema.Items.GetSchemaNames();

        if (!string.IsNullOrEmpty(schema.Reference?.Id))
            return [schema.Reference.Id.Split('/')[^1].Split('.')[^1]];

        List<string> schemaNames = new List<string>();

        if (!directOnly)
        {
            if (schema.AnyOf.Count > 0)
            {
                foreach (var item in schema.AnyOf.FlattenIfRequired(classNamesFlattener))
                {
                    schemaNames.Add(item);
                }
            }

            if (schema.AllOf.Count > 0)
            {
                foreach (var item in schema.AllOf.FlattenIfRequired(classNamesFlattener))
                {
                    schemaNames.Add(item);
                }
            }

            if (schema.OneOf.Count > 0)
            {
                foreach (var item in schema.OneOf.FlattenIfRequired(classNamesFlattener))
                {
                    schemaNames.Add(item);
                }
            }
        }

        return schemaNames;
    }
    internal static IEnumerable<OpenApiSchema> FlattenSchemaIfRequired(this IList<OpenApiSchema> schemas, Func<OpenApiSchema, IList<OpenApiSchema>> subsequentGetter)
    {
        if (schemas is null) return [];
        return schemas.Count == 1 && !schemas[0].HasAnyProperty() ?
                    schemas.FlattenEmptyEntries(subsequentGetter, 1) :
                    schemas;
    }
    private static IEnumerable<string> FlattenIfRequired(this IList<OpenApiSchema> schemas, Func<OpenApiSchema, IList<OpenApiSchema>> subsequentGetter)
    {
        var flattenedSchemas = schemas.FlattenSchemaIfRequired(subsequentGetter);

        foreach (var schema in flattenedSchemas)
        {
            foreach (var name in schema.GetSchemaNames())
            {
                yield return name;
            }
        }
    }
    public static string GetSchemaName(this OpenApiSchema schema, bool directOnly = false)
    {
        var schemaNames = schema.GetSchemaNames(directOnly);
        string lastSchemaName = string.Empty;

        foreach (var name in schemaNames)
        {
            if (name != null)
            {
                lastSchemaName = name;
            }
        }

        return lastSchemaName.TrimStart('$') ?? string.Empty; // OData $ref
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
        if (!"array".Equals(schema?.Type, StringComparison.OrdinalIgnoreCase) || schema.Items == null)
        {
            return false;
        }

        if (schema.Items.IsComposedEnum() || schema.Items.IsEnum() || schema.Items.IsSemanticallyMeaningful())
        {
            return true;
        }

        Func<OpenApiSchema, IList<OpenApiSchema>> subsequentGetter = x =>
        {
            List<OpenApiSchema> schemas = new List<OpenApiSchema>();
            if (x.AnyOf != null)
            {
                foreach (var item in x.AnyOf)
                {
                    schemas.Add(item);
                }
            }
            foreach (var item in x.AllOf)
            {
                schemas.Add(item);
            }
            foreach (var item in x.OneOf)
            {
                schemas.Add(item);
            }
            return schemas;
        };

        OpenApiSchema? firstSchema = null;
        foreach (var item in FlattenEmptyEntries(new List<OpenApiSchema> { schema.Items }, subsequentGetter, 1))
        {
            if (item != null)
            {
                firstSchema = item;
                break;
            }
        }

        return firstSchema is OpenApiSchema flat && flat.IsSemanticallyMeaningful();
    }

    public static bool IsObjectType(this OpenApiSchema? schema) =>
        "object".Equals(schema?.Type, StringComparison.OrdinalIgnoreCase);

    public static bool HasAnyProperty(this OpenApiSchema? schema) => schema?.Properties is { Count: > 0 };

    public static bool IsInclusiveUnion(this OpenApiSchema? schema)
    {
        int meaningfulCount = 0;

        if (schema?.AnyOf != null)
        {
            foreach (var item in schema.AnyOf)
            {
                if (IsSemanticallyMeaningful(item, true))
                {
                    meaningfulCount++;
                }
            }
        }

        return meaningfulCount > 1;
        // so we don't consider any of object/nullable as a union type
    }
    public static bool IsInherited(this OpenApiSchema? schema)
    {
        if (schema is null) return false;

        int countWithId = 0;
        int countWithoutId = 0;
        foreach (var item in schema.AllOf.FlattenSchemaIfRequired(static x => x.AllOf))
        {
            if (item.IsSemanticallyMeaningful(ignoreEnums: true, ignoreArrays: true, ignoreType: true))
            {
                if (string.IsNullOrEmpty(item.Reference?.Id))
                    countWithoutId++;
                else
                    countWithId++;
            }
        }

        var isRootSchemaMeaningful = schema.IsSemanticallyMeaningful(ignoreEnums: true, ignoreArrays: true, ignoreType: true);

        return countWithId == 1 && (countWithoutId == 1 || isRootSchemaMeaningful);
    }
    internal static OpenApiSchema? MergeIntersectionSchemaEntries(this OpenApiSchema? schema, HashSet<OpenApiSchema>? schemasToExclude = default)
    {
        if (schema is null) return null;
        if (!schema.IsIntersection()) return schema;

        var result = new OpenApiSchema(schema);
        result.AllOf.Clear();

        List<OpenApiSchema> meaningfulSchemas = [];
        foreach (var item in schema.AllOf)
        {
            if (item.IsSemanticallyMeaningful() || item.AllOf.Count > 0)
            {
                var merged = MergeIntersectionSchemaEntries(item, schemasToExclude);
                if (merged is not null && (schemasToExclude is null || !schemasToExclude.Contains(merged)))
                {
                    meaningfulSchemas.Add(merged);
                }
            }
        }

        List<OpenApiSchema> flattenedSchemas = [];
        foreach (var item in meaningfulSchemas)
        {
            flattenedSchemas.AddRange(new List<OpenApiSchema> { item }.FlattenEmptyEntries(static x => x.AllOf));
        }
        flattenedSchemas.AddRange(meaningfulSchemas);

        foreach (var item in flattenedSchemas)
        {
            foreach (var property in item.Properties)
            {
                result.Properties.TryAdd(property.Key, property.Value);
            }
        }

        return result;
    }

    public static bool IsIntersection(this OpenApiSchema? schema)
    {
        if (schema?.AllOf == null) return false;

        List<OpenApiSchema> meaningfulSchemas = new List<OpenApiSchema>();
        foreach (var item in schema.AllOf)
        {
            if (item.IsSemanticallyMeaningful())
            {
                meaningfulSchemas.Add(item);
            }
        }

        int countWithId = 0;
        int countWithoutId = 0;
        foreach (var item in meaningfulSchemas)
        {
            if (!string.IsNullOrEmpty(item.Reference?.Id))
                countWithoutId++;
            else
                countWithId++;
        }

        return countWithId > 1 || countWithoutId > 1;
    }

    public static bool IsExclusiveUnion(this OpenApiSchema? schema)
    {
        if (schema?.OneOf == null) return false;

        int count = 0;
        foreach (var item in schema.OneOf)
        {
            if (IsSemanticallyMeaningful(item, true))
            {
                count++;
            }
        }

        return count > 1;
    }
    private static readonly HashSet<string> oDataTypes = new(StringComparer.OrdinalIgnoreCase) {
        "number",
        "integer",
    };
    public static bool IsODataPrimitiveType(this OpenApiSchema schema)
    {
        if (schema.IsExclusiveUnion() && schema.OneOf.Count == 3)
        {
            int enumCount = 0;
            int oDataTypesCount = 0;
            int stringTypeCount = 0;

            foreach (var item in schema.OneOf)
            {
                if (item.Enum?.Count > 0)
                {
                    enumCount++;
                }
                if (oDataTypes.Contains(item.Type))
                {
                    oDataTypesCount++;
                }
                if ("string".Equals(item.Type, StringComparison.OrdinalIgnoreCase))
                {
                    stringTypeCount++;
                }
            }

            if (enumCount == 1 && oDataTypesCount == 1 && stringTypeCount == 1)
            {
                return true;
            }
        }

        if (schema.IsInclusiveUnion() && schema.AnyOf.Count == 3)
        {
            int enumCount = 0;
            int oDataTypesCount = 0;
            int stringTypeCount = 0;

            foreach (var item in schema.AnyOf)
            {
                if (item.Enum?.Count > 0)
                {
                    enumCount++;
                }
                if (oDataTypes.Contains(item.Type))
                {
                    oDataTypesCount++;
                }
                if ("string".Equals(item.Type, StringComparison.OrdinalIgnoreCase))
                {
                    stringTypeCount++;
                }
            }

            if (enumCount == 1 && oDataTypesCount == 1 && stringTypeCount == 1)
            {
                return true;
            }
        }

        return false;
    }
    public static bool IsEnum(this OpenApiSchema schema)
    {
        if (schema is null) return false;

        bool hasNonEmptyString = false;
        foreach (var item in schema.Enum)
        {
            if (item is OpenApiString openApiString && !string.IsNullOrEmpty(openApiString.Value))
            {
                hasNonEmptyString = true;
                break;
            }
        }

        return hasNonEmptyString &&
               (string.IsNullOrEmpty(schema.Type) || "string".Equals(schema.Type, StringComparison.OrdinalIgnoreCase)); // number and boolean enums are not supported
    }
    public static bool IsComposedEnum(this OpenApiSchema schema)
    {
        if (schema is null) return false;

        int nonMeaningfulAnyOfCount = 0;
        int enumAnyOfCount = 0;
        foreach (var item in schema.AnyOf)
        {
            if (!item.IsSemanticallyMeaningful(true))
            {
                nonMeaningfulAnyOfCount++;
            }
            if (item.IsEnum())
            {
                enumAnyOfCount++;
            }
        }

        int nonMeaningfulOneOfCount = 0;
        int enumOneOfCount = 0;
        foreach (var item in schema.OneOf)
        {
            if (!item.IsSemanticallyMeaningful(true))
            {
                nonMeaningfulOneOfCount++;
            }
            if (item.IsEnum())
            {
                enumOneOfCount++;
            }
        }

        return (nonMeaningfulAnyOfCount == 1 && enumAnyOfCount == 1) || (nonMeaningfulOneOfCount == 1 && enumOneOfCount == 1);
    }
    public static bool IsSemanticallyMeaningful(this OpenApiSchema schema, bool ignoreNullableObjects = false, bool ignoreEnums = false, bool ignoreArrays = false, bool ignoreType = false)
    {
        if (schema is null) return false;
        return schema.HasAnyProperty() ||
                (!ignoreEnums && schema.Enum is { Count: > 0 }) ||
                (!ignoreArrays && schema.Items != null) ||
                (!ignoreType && !string.IsNullOrEmpty(schema.Type) &&
                    ((ignoreNullableObjects && !"object".Equals(schema.Type, StringComparison.OrdinalIgnoreCase)) ||
                    !ignoreNullableObjects)) ||
                !string.IsNullOrEmpty(schema.Format) ||
                !string.IsNullOrEmpty(schema.Reference?.Id);
    }
    public static IEnumerable<string> GetSchemaReferenceIds(this OpenApiSchema schema, HashSet<OpenApiSchema>? visitedSchemas = null)
    {
        visitedSchemas ??= [];
        if (schema != null && !visitedSchemas.Contains(schema))
        {
            visitedSchemas.Add(schema);
            var result = new HashSet<string>();
            if (!string.IsNullOrEmpty(schema.Reference?.Id))
                result.Add(schema.Reference.Id);
            if (schema.Items != null)
            {
                if (!string.IsNullOrEmpty(schema.Items.Reference?.Id))
                    result.Add(schema.Items.Reference.Id);
                foreach (var id in schema.Items.GetSchemaReferenceIds(visitedSchemas))
                {
                    result.Add(id);
                }
            }

            var subSchemas = new List<OpenApiSchema>();
            if (schema.Properties?.Values != null)
                subSchemas.AddRange(schema.Properties.Values);
            if (schema.AnyOf != null)
                subSchemas.AddRange(schema.AnyOf);
            if (schema.AllOf != null)
                subSchemas.AddRange(schema.AllOf);
            if (schema.OneOf != null)
                subSchemas.AddRange(schema.OneOf);

            foreach (var subSchema in subSchemas)
            {
                foreach (var id in subSchema.GetSchemaReferenceIds(visitedSchemas))
                {
                    result.Add(id);
                }
            }

            return result;
        }

        return [];
    }
    private static IEnumerable<OpenApiSchema> FlattenEmptyEntries(this IEnumerable<OpenApiSchema> schemas, Func<OpenApiSchema, IList<OpenApiSchema>> subsequentGetter, int? maxDepth = default)
    {
        if (schemas == null) return Array.Empty<OpenApiSchema>();
        ArgumentNullException.ThrowIfNull(subsequentGetter);

        if ((maxDepth ?? 1) <= 0)
            return schemas;

        var result = new List<OpenApiSchema>(schemas);
        var permutations = new Dictionary<OpenApiSchema, IEnumerable<OpenApiSchema>>();
        foreach (var item in result)
        {
            var subsequentItems = subsequentGetter(item);
            bool hasAny = false;
            foreach (var subItem in subsequentItems)
            {
                hasAny = true;
                break;
            }
            if (hasAny)
                permutations.Add(item, subsequentItems.FlattenEmptyEntries(subsequentGetter, maxDepth.HasValue ? --maxDepth : default));
        }
        if (permutations.Count > 0)
        {
            foreach (var permutation in permutations)
            {
                int index = -1;
                for (int i = 0; i < result.Count; i++)
                {
                    if (result[i].Equals(permutation.Key))
                    {
                        index = i;
                        break;
                    }
                }
                if (index != -1)
                {
                    result.RemoveAt(index);
                    var offset = 0;
                    foreach (var insertee in permutation.Value)
                    {
                        result.Insert(index + offset, insertee);
                        offset++;
                    }
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

        foreach (var oneOfSchema in schema.OneOf)
        {
            var discriminatorPropertyName = GetDiscriminatorPropertyName(oneOfSchema);
            if (!string.IsNullOrEmpty(discriminatorPropertyName))
                return discriminatorPropertyName;
        }

        foreach (var anyOfSchema in schema.AnyOf)
        {
            var discriminatorPropertyName = GetDiscriminatorPropertyName(anyOfSchema);
            if (!string.IsNullOrEmpty(discriminatorPropertyName))
                return discriminatorPropertyName;
        }

        if (schema.AllOf.Count > 0)
            return GetDiscriminatorPropertyName(schema.AllOf[^1]);

        return string.Empty;
    }
    internal static IEnumerable<KeyValuePair<string, string>> GetDiscriminatorMappings(this OpenApiSchema schema, ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> inheritanceIndex)
    {
        if (schema == null)
            return [];

        if (schema.Discriminator?.Mapping?.Count > 0)
            return schema.Discriminator.Mapping;

        var result = new List<KeyValuePair<string, string>>();
        if (schema.OneOf.Count > 0)
        {
            foreach (var oneOfSchema in schema.OneOf)
            {
                result.AddRange(GetDiscriminatorMappings(oneOfSchema, inheritanceIndex));
            }
        }
        else if (schema.AnyOf.Count > 0)
        {
            foreach (var anyOfSchema in schema.AnyOf)
            {
                result.AddRange(GetDiscriminatorMappings(anyOfSchema, inheritanceIndex));
            }
        }
        else if (schema.AllOf.Count > 0)
        {
            OpenApiSchema? lastAllOfSchema = null;
            for (int i = schema.AllOf.Count - 1; i >= 0; i--)
            {
                if (allOfEvaluatorForMappings(schema.AllOf[i]))
                {
                    lastAllOfSchema = schema.AllOf[i];
                    break;
                }
            }
            if (lastAllOfSchema != null && schema.AllOf[^1].Equals(lastAllOfSchema))
            {
                result.AddRange(GetDiscriminatorMappings(lastAllOfSchema, inheritanceIndex));
            }
        }
        else if (!string.IsNullOrEmpty(schema.Reference?.Id))
        {
            foreach (var reference in GetAllInheritanceSchemaReferences(schema.Reference.Id, inheritanceIndex))
            {
                if (!string.IsNullOrEmpty(reference))
                {
                    result.Add(KeyValuePair.Create(reference, reference));
                }
            }
            result.Add(KeyValuePair.Create(schema.Reference.Id, schema.Reference.Id));
        }

        return result;
    }
    private static readonly Func<OpenApiSchema, bool> allOfEvaluatorForMappings = static x => x.Discriminator?.Mapping.Count > 0;
    private static IEnumerable<string> GetAllInheritanceSchemaReferences(string currentReferenceId, ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> inheritanceIndex)
    {
        ArgumentException.ThrowIfNullOrEmpty(currentReferenceId);
        ArgumentNullException.ThrowIfNull(inheritanceIndex);

        if (inheritanceIndex.TryGetValue(currentReferenceId, out var dependents))
        {
            foreach (var key in dependents.Keys)
            {
                yield return key;
                foreach (var reference in GetAllInheritanceSchemaReferences(key, inheritanceIndex))
                {
                    yield return reference;
                }
            }
        }
    }
}

