using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Extensions {
    public static class OpenApiUrlSpaceNodeExtensions {

        // where component id and the value is the set of openapiurlNode referencing it
        public static Dictionary<string, HashSet<OpenApiUrlSpaceNode>> GetComponentsReferenceIndex(this OpenApiUrlSpaceNode rootNode) {
            var result = new Dictionary<string, HashSet<OpenApiUrlSpaceNode>>();
            AddAllPathsEntries(rootNode, result);
            return result;
        }
        private static void AddAllPathsEntries(OpenApiUrlSpaceNode currentNode, Dictionary<string, HashSet<OpenApiUrlSpaceNode>> index) {
            if(currentNode == null)
                return;
            
            if(currentNode.PathItem != null && currentNode.HasOperations()) {
                var nodeOperations = currentNode.PathItem.Operations.Values;
                var requestSchemasFirstLevel = nodeOperations.SelectMany(x => x.RequestBody?.Content?.Values?.Select(y => y.Schema) ?? Enumerable.Empty<OpenApiSchema>());
                var responseSchemasFirstLevel = nodeOperations.SelectMany(x => 
                                                    x?.Responses?.Values?.SelectMany(y => 
                                                                    y?.Content?.Values?.Select(z => z.Schema) ?? Enumerable.Empty<OpenApiSchema>()) ?? Enumerable.Empty<OpenApiSchema>());
                var operationFirstLevelSchemas = requestSchemasFirstLevel.Union(responseSchemasFirstLevel);

                operationFirstLevelSchemas.SelectMany(x => GetSchemaReferenceIds(x)).ToList().ForEach(x => {
                    if(index.TryGetValue(x, out var entry))
                        entry.Add(currentNode);
                    else
                        index.Add(x, new(new [] { currentNode}));
                });
            }
            
            if(currentNode.Children != null)
                foreach(var child in currentNode.Children.Values)
                    AddAllPathsEntries(child, index);
        }
        private static IEnumerable<string> GetSchemaReferenceIds(OpenApiSchema schema, HashSet<OpenApiSchema> visitedSchemas = null) {
            if(visitedSchemas == null)
                visitedSchemas = new();            
            if(!visitedSchemas.Contains(schema)) {
                visitedSchemas.Add(schema);
                var result = new List<string>();
                if(!string.IsNullOrEmpty(schema.Reference?.Id))
                    result.Add(schema.Reference.Id);
                if(schema.Properties != null) {
                    schema.Properties.Values.ToList().ForEach(x => visitedSchemas.Add(x));
                    result.AddRange(schema.Properties.Values.SelectMany(x => GetSchemaReferenceIds(x, visitedSchemas)));
                }
                if(schema.AnyOf != null) {
                    schema.AnyOf.ToList().ForEach(x => visitedSchemas.Add(x));
                    result.AddRange(schema.AnyOf.SelectMany(x => GetSchemaReferenceIds(x, visitedSchemas)));
                }
                if(schema.AllOf != null) {
                    schema.AllOf.ToList().ForEach(x => visitedSchemas.Add(x));
                    result.AddRange(schema.AllOf.SelectMany(x => GetSchemaReferenceIds(x, visitedSchemas)));
                }
                if(schema.OneOf != null) {
                    schema.OneOf.ToList().ForEach(x => visitedSchemas.Add(x));
                    result.AddRange(schema.OneOf.SelectMany(x => GetSchemaReferenceIds(x, visitedSchemas)));
                }
                return result;
            } else 
                return Enumerable.Empty<string>();
        }
    }
}
