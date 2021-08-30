using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace Kiota.Builder.Extensions {
    public static class OpenApiUrlTreeNodeExtensions {

        // where component id and the value is the set of openapiurlNode referencing it
        public static Dictionary<string, HashSet<OpenApiUrlTreeNode>> GetComponentsReferenceIndex(this OpenApiUrlTreeNode rootNode, string label) {
            var result = new Dictionary<string, HashSet<OpenApiUrlTreeNode>>(StringComparer.OrdinalIgnoreCase);
            AddAllPathsEntries(rootNode, result, label);
            return result;
        }
        private static void AddAllPathsEntries(OpenApiUrlTreeNode currentNode, Dictionary<string, HashSet<OpenApiUrlTreeNode>> index, string label) {
            if(currentNode == null || string.IsNullOrEmpty(label))
                return;
            
            if(currentNode.PathItems.ContainsKey(label) && currentNode.HasOperations(label)) {
                var nodeOperations = currentNode.PathItems[label].Operations.Values;
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
                    AddAllPathsEntries(child, index, label);
        }
        public static string GetNodeNamespaceFromPath(this OpenApiUrlTreeNode currentNode, string prefix) =>
            prefix + 
                    ((currentNode?.Path?.Contains(pathNameSeparator) ?? false) ?
                        (string.IsNullOrEmpty(prefix) ? string.Empty : ".")
                             + currentNode?.Path
                                ?.Split(pathNameSeparator, StringSplitOptions.RemoveEmptyEntries)
                                ?.Where(x => !x.StartsWith('{'))
                                ?.Aggregate(string.Empty, 
                                    (x, y) => $"{x}{(string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y) ? string.Empty : ".")}{y}") :
                        string.Empty)
                    .ReplaceValueIdentifier();
        private static readonly char pathNameSeparator = '\\';
        private static readonly Regex idClassNameCleanup = new Regex(@"Id\d?$");
        ///<summary>
        /// Returns the class name for the node with more or less precision depending on the provided arguments
        ///</summary>
        public static string GetClassName(this OpenApiUrlTreeNode currentNode, string suffix = default, string prefix = default, OpenApiOperation operation = default) {
            var rawClassName = operation?.GetResponseSchema()?.Reference?.GetClassName() ?? 
                                currentNode?.GetIdentifier()?.ReplaceValueIdentifier();
            if((currentNode?.DoesNodeBelongToItemSubnamespace() ?? false) && idClassNameCleanup.IsMatch(rawClassName))
                rawClassName = idClassNameCleanup.Replace(rawClassName, string.Empty);
            return prefix + rawClassName + suffix;
        }
        public static string GetPathItemDescription(this OpenApiUrlTreeNode currentNode, string label, string defaultValue = default) =>
        !string.IsNullOrEmpty(label) && (currentNode?.PathItems.ContainsKey(label) ?? false) ?
                currentNode.PathItems[label].Description ??
                currentNode.PathItems[label].Summary ??
                defaultValue :
            defaultValue;
        public static bool DoesNodeBelongToItemSubnamespace(this OpenApiUrlTreeNode currentNode) =>
        (currentNode?.Segment.StartsWith("{") ?? false) && currentNode.Segment.EndsWith("}");
        public static bool IsParameter(this OpenApiUrlTreeNode currentNode)
        {
            return currentNode?.Segment.StartsWith("{") ?? false;
        }
        public static bool IsFunction(this OpenApiUrlTreeNode currentNode)
        {
            return currentNode?.Segment.Contains("(") ?? false;
        }
        public static string GetIdentifier(this OpenApiUrlTreeNode currentNode)
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
