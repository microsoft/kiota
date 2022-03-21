using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Extensions {
    public static class OpenApiSchemaExtensions {
        private static readonly Func<OpenApiSchema, IList<OpenApiSchema>> classNamesFlattener = (x) =>
        (x.AnyOf ?? Enumerable.Empty<OpenApiSchema>()).Union(x.AllOf).Union(x.OneOf).ToList();
        public static IEnumerable<string> GetSchemaTitles(this OpenApiSchema schema) {
            if(schema == null)
                return Enumerable.Empty<string>();
            else if(schema.Items != null)
                return schema.Items.GetSchemaTitles();
            else if(!string.IsNullOrEmpty(schema.Title))
                return new string[] { schema.Title };
            else if(schema.AnyOf.Any())
                return schema.AnyOf.FlattenIfRequired(classNamesFlattener);
            else if(schema.AllOf.Any())
                return schema.AllOf.FlattenIfRequired(classNamesFlattener);
            else if(schema.OneOf.Any())
                return schema.OneOf.FlattenIfRequired(classNamesFlattener);
            else if(!string.IsNullOrEmpty(schema.Reference?.Id))
                return new string[] {schema.Reference.Id.Split('/').Last().Split('.').Last()};
            else if(!string.IsNullOrEmpty(schema.Xml?.Name))
                return new string[] {schema.Xml.Name};
            else return Enumerable.Empty<string>();
        }
        public static IEnumerable<OpenApiSchema> GetNonEmptySchemas(this OpenApiSchema schema) {
            if(schema == null) return Enumerable.Empty<OpenApiSchema>();
            else if(!string.IsNullOrEmpty(schema.Reference?.Id)) return new OpenApiSchema[] { schema };
            else if(schema.Properties?.Any() ?? false) return new OpenApiSchema[] { schema };
            else if(schema.Items != null) return schema.Items.GetNonEmptySchemas();
            else if(schema.AnyOf.Any()) return schema.AnyOf.SelectMany(x => x.GetNonEmptySchemas());
            else if(schema.AllOf.Any()) return schema.AllOf.SelectMany(x => x.GetNonEmptySchemas());
            else if(schema.OneOf.Any()) return schema.OneOf.SelectMany(x => x.GetNonEmptySchemas());
            else return Enumerable.Empty<OpenApiSchema>();            
        }
        private static IEnumerable<string> FlattenIfRequired(this IList<OpenApiSchema> schemas, Func<OpenApiSchema, IList<OpenApiSchema>> subsequentGetter) {
            var resultSet = schemas;
            if(schemas.Count == 1 && string.IsNullOrEmpty(schemas.First().Title))
                resultSet = schemas.FlattenEmptyEntries(subsequentGetter, 1);
            
            return resultSet.Select(x => x.Title).Where(x => !string.IsNullOrEmpty(x));
        }

        public static string GetSchemaTitle(this OpenApiSchema schema) {
            return schema.GetSchemaTitles().LastOrDefault()?.TrimStart('$');// OData $ref
        }

        public static bool IsReferencedSchema(this OpenApiSchema schema) {
            return schema?.Reference != null;
        }

        public static bool IsArray(this OpenApiSchema schema)
        {
            return schema?.Type?.Equals("array", StringComparison.OrdinalIgnoreCase) ?? false;
        }

        public static bool IsObject(this OpenApiSchema schema)
        {
            return schema?.Type?.Equals("object", StringComparison.OrdinalIgnoreCase) ?? false;
        }
        public static bool IsAnyOf(this OpenApiSchema schema)
        {
            return schema?.AnyOf?.Any() ?? false;
        }

        public static bool IsAllOf(this OpenApiSchema schema)
        {
            return schema?.AllOf?.Any() ?? false;
        }

        public static bool IsOneOf(this OpenApiSchema schema)
        {
            return schema?.OneOf?.Any() ?? false;
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
            } else 
                return Enumerable.Empty<string>();
        }
        internal static IList<OpenApiSchema> FlattenEmptyEntries(this IList<OpenApiSchema> schemas, Func<OpenApiSchema, IList<OpenApiSchema>> subsequentGetter, int? maxDepth = default) {
            if(schemas == null) return default;
            if(subsequentGetter == null) throw new ArgumentNullException(nameof(subsequentGetter));

            if((maxDepth ?? 1) <= 0)
                return schemas;

            var result = schemas.ToList();
            var permutations = new Dictionary<OpenApiSchema, IList<OpenApiSchema>>();
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
    }
}
