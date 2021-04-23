using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Extensions {
    public static class OpenApiSchemaExtensions {
        internal static IEnumerable<string> GetClassNames(this OpenApiSchema schema) {
            if(schema.Items != null)
                return schema.Items.GetClassNames();
            else if(schema.AnyOf.Any())
                return schema.AnyOf.Select(x => x.Title);
            else if(schema.AllOf.Any())
                return schema.AllOf.Select(x => x.Title);
            else if(schema.OneOf.Any())
                return schema.OneOf.Select(x => x.Title);
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
        internal static IList<OpenApiSchema> FlattenEmptyAllOf(this OpenApiSchema schema) {
            var result = schema.AllOf.ToList();
            var permutations = new Dictionary<OpenApiSchema, IList<OpenApiSchema>>();
            foreach(var allOfItem in result)
                if(string.IsNullOrEmpty(allOfItem.Title) && (allOfItem.AllOf?.Any() ?? false))
                    permutations.Add(allOfItem, allOfItem.FlattenEmptyAllOf());
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
