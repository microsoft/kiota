using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Extensions {
    public static class OpenApiUrlSpaceNodeExtensions {

        // where component id and the value is the set of openapiurlNode referencing it
        public static Dictionary<string, HashSet<OpenApiUrlSpaceNode>> GetComponentsReferenceIndex(this OpenApiUrlSpaceNode rootNode) {
            var result = new Dictionary<string, HashSet<OpenApiUrlSpaceNode>>(StringComparer.OrdinalIgnoreCase);
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

                operationFirstLevelSchemas.SelectMany(x => x.GetSchemaReferenceIds()).ToList().ForEach(x => {
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
        internal static string GetNodeNamespaceFromPath(this OpenApiUrlSpaceNode currentNode, string prefix) =>
            prefix + 
                    ((currentNode?.Path?.Contains(pathNameSeparator) ?? false) ?
                        "." + currentNode?.Path
                                ?.Split(pathNameSeparator, StringSplitOptions.RemoveEmptyEntries)
                                ?.Where(x => !x.StartsWith('{'))
                                ?.Aggregate((x, y) => $"{x}.{y}") :
                        string.Empty)
                    .ReplaceValueIdentifier();
        private static readonly char pathNameSeparator = '\\';
        private static readonly Regex idClassNameCleanup = new Regex(@"Id\d?$");
        ///<summary>
        /// Returns the class name for the node with more or less precision depending on the provided arguments
        ///</summary>
        internal static string GetClassName(this OpenApiUrlSpaceNode currentNode, string suffix = default, string prefix = default, OpenApiOperation operation = default) {
            var rawClassName = operation?.GetResponseSchema()?.Reference?.GetClassName() ?? 
                                currentNode?.GetIdentifier()?.ReplaceValueIdentifier();
            if(currentNode?.DoesNodeBelongToItemSubnamespace() ?? false && idClassNameCleanup.IsMatch(rawClassName))
                rawClassName = idClassNameCleanup.Replace(rawClassName, string.Empty);
            return prefix + rawClassName + suffix;
        }
        internal static bool DoesNodeBelongToItemSubnamespace(this OpenApiUrlSpaceNode currentNode) =>
        (currentNode?.Segment?.StartsWith("{") ?? false) && (currentNode?.Segment?.EndsWith("}") ?? false);
        internal static bool HasOperations(this OpenApiUrlSpaceNode currentNode) => currentNode?.PathItem?.Operations?.Any() ?? false;
        internal static bool IsParameter(this OpenApiUrlSpaceNode currentNode)
        {
            return currentNode?.Segment?.StartsWith("{") ?? false;
        }
        internal static bool IsFunction(this OpenApiUrlSpaceNode currentNode)
        {
            return currentNode?.Segment?.Contains("(") ?? false;
        }
        internal static string GetIdentifier(this OpenApiUrlSpaceNode currentNode)
        {
            if(currentNode == null) return string.Empty;
            string identifier;
            if (currentNode.IsParameter())
            {
                identifier = currentNode.Segment.Substring(1, currentNode.Segment.Length - 2).ToPascalCase();
            }
            else
            {
                identifier = currentNode.Segment.ToPascalCase().Replace("()", "");
                var openParen = identifier.IndexOf("(");
                if (openParen >= 0)
                {
                    identifier = identifier.Substring(0, openParen);
                }
            }
            return identifier;
        }
    }
}
