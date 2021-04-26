using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Extensions {
    public static class OpenApiSchemaExtensions {
        internal static IEnumerable<string> GetClassNames(this OpenApiSchema schema) {
            if(schema.Items != null)
                return schema.Items.GetClassNames();
            else if(schema.AnyOf.Any())
                return schema.AnyOf.FlattenEmptyEntries(x => x.AnyOf).Select(x => x.Title);
            else if(schema.AllOf.Any())
                return schema.AllOf.FlattenEmptyEntries(x => x.AllOf).Select(x => x.Title);
            else if(schema.OneOf.Any())
                return schema.OneOf.FlattenEmptyEntries(x => x.OneOf).Select(x => x.Title);
            else if(!string.IsNullOrEmpty(schema.Title))
                return new List<string>{ schema.Title };
            else return Enumerable.Empty<string>();
        }
        internal static string GetClassName(this OpenApiSchema schema) {
            return schema.GetClassNames().LastOrDefault();
        }
        internal static IEnumerable<string> GetSchemaReferenceIds(this OpenApiSchema schema, HashSet<OpenApiSchema> visitedSchemas = null) {
            if(visitedSchemas == null)
                visitedSchemas = new();            
            if(!visitedSchemas.Contains(schema)) {
                visitedSchemas.Add(schema);
                var result = new List<string>();
                if(!string.IsNullOrEmpty(schema.Reference?.Id))
                    result.Add(schema.Reference.Id);
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
        internal static IList<OpenApiSchema> FlattenEmptyEntries(this IList<OpenApiSchema> schemas, Func<OpenApiSchema, IList<OpenApiSchema>> subsequentGetter) {
            if(schemas == null) return default;
            if(subsequentGetter == null) throw new ArgumentNullException(nameof(subsequentGetter));
            
            var result = schemas.ToList();
            var permutations = new Dictionary<OpenApiSchema, IList<OpenApiSchema>>();
            foreach(var item in result)
            {
                var subsequentItems = subsequentGetter(item);
                if(string.IsNullOrEmpty(item.Title) && subsequentItems.Any())
                    permutations.Add(item, subsequentItems.FlattenEmptyEntries(subsequentGetter));
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
