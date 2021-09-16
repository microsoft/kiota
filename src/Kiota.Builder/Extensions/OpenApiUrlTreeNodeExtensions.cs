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
        private static string GetDotIfBothNotNullOfEmpty(string x, string y) => string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y) ? string.Empty : ".";
        public static string GetNamespaceFromPath(this string currentPath, string prefix) => 
            prefix + 
                    ((currentPath?.Contains(pathNameSeparator) ?? false) ?
                        (string.IsNullOrEmpty(prefix) ? string.Empty : ".")
                             + currentPath
                                ?.Split(pathNameSeparator, StringSplitOptions.RemoveEmptyEntries)
                                ?.Select(x => x.IsPathSegmentWithSingleSimpleParamter() ? "item" : x)
                                ?.Select(x => CleanupParametersFromPath((x ?? string.Empty).Split('.', StringSplitOptions.RemoveEmptyEntries)
                                                                .Last()))
                                ?.Aggregate(string.Empty, 
                                    (x, y) => $"{x}{GetDotIfBothNotNullOfEmpty(x, y)}{y}") :
                        string.Empty)
                    .ReplaceValueIdentifier();
        public static string GetNodeNamespaceFromPath(this OpenApiUrlTreeNode currentNode, string prefix) =>
            currentNode?.Path?.GetNamespaceFromPath(prefix);
        //{id}, name(idParam={id}), name(idParam='{id}'), name(idParam='{id}',idParam2='{id2}')
        private static readonly Regex PathParametersRegex = new(@"(?:\w+)?=?'?\{(?<paramName>\w+)\}'?,?", RegexOptions.Compiled);
        private static readonly char requestParametersChar = '{';
        private static readonly char requestParametersEndChar = '}';
        private static readonly char requestParametersSectionChar = '(';
        private static readonly char requestParametersSectionEndChar = ')';
        private static readonly MatchEvaluator requestParametersMatchEvaluator = (match) => {
            return "With" + match.Groups["paramName"].Value.ToFirstCharacterUpperCase();
        };
        private static string CleanupParametersFromPath(string pathSegment) {
            if((pathSegment?.Contains(requestParametersChar) ?? false) ||
                (pathSegment?.Contains(requestParametersSectionChar) ?? false))
                return PathParametersRegex.Replace(pathSegment, requestParametersMatchEvaluator)
                                        .TrimEnd(requestParametersSectionEndChar)
                                        .Replace(requestParametersSectionChar.ToString(), string.Empty);
            return pathSegment;
        }
        public static IEnumerable<OpenApiParameter> GetPathParametersForCurrentSegment(this OpenApiUrlTreeNode node) {
            if(node != null &&
                (node.Segment.Contains(requestParametersSectionChar) || node.Segment.Count(x => x == requestParametersChar) > 1) &&
                node.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pathItem))
                return pathItem.Parameters
                                .Union(pathItem.Operations.SelectMany(x => x.Value.Parameters))
                                .Where(x => x.In == ParameterLocation.Path)
                                .Where(x => node.Segment.Contains($"{{{x.Name}}}", StringComparison.OrdinalIgnoreCase));
            return Enumerable.Empty<OpenApiParameter>();
        }
        private static readonly char pathNameSeparator = '\\';
        private static readonly Regex idClassNameCleanup = new(@"Id\d?$", RegexOptions.Compiled);
        ///<summary>
        /// Returns the class name for the node with more or less precision depending on the provided arguments
        ///</summary>
        public static string GetClassName(this OpenApiUrlTreeNode currentNode, string suffix = default, string prefix = default, OpenApiOperation operation = default) {
            var rawClassName = (operation?.GetResponseSchema()?.Reference?.GetClassName() ?? 
                                CleanupParametersFromPath(currentNode.Segment)?.ReplaceValueIdentifier())
                                .TrimEnd(requestParametersEndChar)
                                .TrimStart(requestParametersChar)
                                .Split('-')
                                .First();
            if((currentNode?.DoesNodeBelongToItemSubnamespace() ?? false) && idClassNameCleanup.IsMatch(rawClassName))
                rawClassName = idClassNameCleanup.Replace(rawClassName, string.Empty);
            return prefix + rawClassName?.Split('.', StringSplitOptions.RemoveEmptyEntries)?.LastOrDefault() + suffix;
        }
        public static string GetPathItemDescription(this OpenApiUrlTreeNode currentNode, string label, string defaultValue = default) =>
        !string.IsNullOrEmpty(label) && (currentNode?.PathItems.ContainsKey(label) ?? false) ?
                currentNode.PathItems[label].Description ??
                currentNode.PathItems[label].Summary ??
                defaultValue :
            defaultValue;
        public static bool DoesNodeBelongToItemSubnamespace(this OpenApiUrlTreeNode currentNode) => currentNode.IsPathSegmentWithSingleSimpleParamter();
        public static bool IsPathSegmentWithSingleSimpleParamter(this OpenApiUrlTreeNode currentNode) =>
            currentNode?.Segment.IsPathSegmentWithSingleSimpleParamter() ?? false;
        public static bool IsPathSegmentWithSingleSimpleParamter(this string currentSegment)
        {
            return (currentSegment?.StartsWith(requestParametersChar) ?? false) &&
                    currentSegment.EndsWith(requestParametersEndChar) &&
                    currentSegment.Count(x => x == requestParametersChar) == 1;
        }
        public static bool IsComplexPathWithAnyNumberOfParameters(this OpenApiUrlTreeNode currentNode)
        {
            return (currentNode?.Segment?.Contains(requestParametersSectionChar) ?? false) && currentNode.Segment.EndsWith(requestParametersSectionEndChar);
        }
    }
}
