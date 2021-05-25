using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Extensions {
    public static class OpenApiSchemaExtensions {
        private static Func<OpenApiSchema, IList<OpenApiSchema>> classNamesFlattener = (x) =>
        (x.AnyOf ?? Enumerable.Empty<OpenApiSchema>()).Union(x.AllOf).Union(x.OneOf).ToList();
        public static IEnumerable<string> GetSchemaTitles(this OpenApiSchema schema) {
            if(schema.Items != null)
                return schema.Items.GetSchemaTitles();
            else if(!string.IsNullOrEmpty(schema.Title))
                return new List<string>{ schema.Title };
            else if(schema.AnyOf.Any())
                return schema.AnyOf.FlattenIfRequired(classNamesFlattener);
            else if(schema.AllOf.Any())
                return schema.AllOf.FlattenIfRequired(classNamesFlattener);
            else if(schema.OneOf.Any())
                return schema.OneOf.FlattenIfRequired(classNamesFlattener);
            else return Enumerable.Empty<string>();
        }
        private static IEnumerable<string> FlattenIfRequired(this IList<OpenApiSchema> schemas, Func<OpenApiSchema, IList<OpenApiSchema>> subsequentGetter) {
            var resultSet = schemas;
            if(schemas.Count == 1 && string.IsNullOrEmpty(schemas.First().Title))
                resultSet = schemas.FlattenEmptyEntries(subsequentGetter, 1);
            
            return resultSet.Select(x => x.Title).Where(x => !string.IsNullOrEmpty(x));
        }

        public static string GetSchemaTitle(this OpenApiSchema schema) {
            return schema.GetSchemaTitles().LastOrDefault();
        }
        public static IEnumerable<string> GetSchemaReferenceIds(this OpenApiSchema schema, HashSet<OpenApiSchema> visitedSchemas = null) {
            if(visitedSchemas == null)
                visitedSchemas = new();            
            if(!visitedSchemas.Contains(schema)) {
                visitedSchemas.Add(schema);
                var result = new List<string>();
                if(!string.IsNullOrEmpty(schema.Reference?.Id))
                    result.Add(schema.Reference.Id);
                if(!string.IsNullOrEmpty(schema.Items?.Reference?.Id))
                    result.Add(schema.Items.Reference.Id);
                if(schema.Properties != null)
                    result.AddRange(schema.Properties.Values.SelectMany(x => x.GetSchemaReferenceIds(visitedSchemas)));
                if(schema.AnyOf != null)
                    result.AddRange(schema.AnyOf.SelectMany(x => x.GetSchemaReferenceIds(visitedSchemas)));
                if(schema.AllOf != null)
                    result.AddRange(schema.AllOf.SelectMany(x => x.GetSchemaReferenceIds(visitedSchemas)));
                if(schema.OneOf != null)
                    result.AddRange(schema.OneOf.SelectMany(x => x.GetSchemaReferenceIds(visitedSchemas)));
                return result;
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
