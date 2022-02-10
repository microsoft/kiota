using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace Kiota.Builder.Extensions {
    public static class OpenApiUrlTreeNodeExtensions {
        private static string GetDotIfBothNotNullOfEmpty(string x, string y) => string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y) ? string.Empty : ".";
        private static readonly Func<string, string> replaceSingleParameterSegementByItem =
        x => x.IsPathSegmentWithSingleSimpleParameter() ? "item" : x;
        public static string GetNamespaceFromPath(this string currentPath, string prefix) => 
            prefix + 
                    ((currentPath?.Contains(pathNameSeparator) ?? false) ?
                        (string.IsNullOrEmpty(prefix) ? string.Empty : ".")
                             + currentPath
                                ?.Split(pathNameSeparator, StringSplitOptions.RemoveEmptyEntries)
                                ?.Select(replaceSingleParameterSegementByItem)
                                ?.Select(x => CleanupParametersFromPath((x ?? string.Empty).Split('.', StringSplitOptions.RemoveEmptyEntries)
                                ?.Select(x => x.TrimStart('$')) //$ref from OData
                                                                .Last()))
                                ?.Aggregate(string.Empty, 
                                    (x, y) => $"{x}{GetDotIfBothNotNullOfEmpty(x, y)}{y}") :
                        string.Empty)
                    .ReplaceValueIdentifier();
        public static string GetNodeNamespaceFromPath(this OpenApiUrlTreeNode currentNode, string prefix) =>
            currentNode?.Path?.GetNamespaceFromPath(prefix);
        //{id}, name(idParam={id}), name(idParam='{id}'), name(idParam='{id}',idParam2='{id2}')
        private static readonly Regex PathParametersRegex = new(@"(?:\w+)?=?'?\{(?<paramName>\w+)\}'?,?", RegexOptions.Compiled);
        // microsoft.graph.getRoleScopeTagsByIds(ids=@ids)
        private static readonly Regex AtSignPathParameterRegex = new(@"=@(\w+)", RegexOptions.Compiled);
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
                return PathParametersRegex.Replace(
                                            AtSignPathParameterRegex.Replace(pathSegment, "={$1}"),
                                        requestParametersMatchEvaluator)
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
        public static string GetClassName(this OpenApiUrlTreeNode currentNode, string suffix = default, string prefix = default, OpenApiOperation operation = default, OpenApiResponse response = default, OpenApiSchema schema = default) {
            var rawClassName = (schema?.Reference?.GetClassName() ??
                                response?.GetResponseSchema()?.Reference?.GetClassName() ??
                                operation?.GetResponseSchema()?.Reference?.GetClassName() ?? 
                                CleanupParametersFromPath(currentNode.Segment)?.ReplaceValueIdentifier())
                                .TrimEnd(requestParametersEndChar)
                                .TrimStart(requestParametersChar)
                                .TrimStart('$') //$ref from OData
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
        public static bool DoesNodeBelongToItemSubnamespace(this OpenApiUrlTreeNode currentNode) => currentNode.IsPathSegmentWithSingleSimpleParameter();
        public static bool IsPathSegmentWithSingleSimpleParameter(this OpenApiUrlTreeNode currentNode) =>
            currentNode?.Segment.IsPathSegmentWithSingleSimpleParameter() ?? false;
        private static bool IsPathSegmentWithSingleSimpleParameter(this string currentSegment)
        {
            return (currentSegment?.StartsWith(requestParametersChar) ?? false) &&
                    currentSegment.EndsWith(requestParametersEndChar) &&
                    currentSegment.Count(x => x == requestParametersChar) == 1;
        }
        public static bool IsComplexPathWithAnyNumberOfParameters(this OpenApiUrlTreeNode currentNode)
        {
            return (currentNode?.Segment?.Contains(requestParametersSectionChar) ?? false) && currentNode.Segment.EndsWith(requestParametersSectionEndChar);
        }
        public static string GetUrlTemplate(this OpenApiUrlTreeNode currentNode) {
            var queryStringParameters = string.Empty;
            if(currentNode.HasOperations(Constants.DefaultOpenApiLabel))
            {
                var pathItem = currentNode.PathItems[Constants.DefaultOpenApiLabel];
                var parameters = pathItem.Parameters.Where(x => x.In == ParameterLocation.Query).ToList();
                parameters.AddRange(pathItem.Operations.SelectMany(x => x.Value.Parameters).Where(x => x.In == ParameterLocation.Query));
                if(parameters.Any())
                    queryStringParameters = "{?" + 
                                            parameters.Select(x => 
                                                                x.Name.TrimStart('$') +
                                                                (x.Explode ? 
                                                                    "*" : string.Empty))
                                                    .Aggregate((x, y) => $"{x},{y}") +
                                            '}';
            }
            return "{+baseurl}" + 
                    SanitizePathParameterNames(currentNode.Path.Replace('\\', '/')) +
                    queryStringParameters;
        }
        private static readonly Regex pathParamMatcher = new(@"{[\w-]+}",RegexOptions.Compiled);
        private static string SanitizePathParameterNames(string original) {
            if(string.IsNullOrEmpty(original) || !original.Contains('{')) return original;
            var parameters = pathParamMatcher.Matches(original);
            foreach(var value in parameters.Select(x => x.Value))
                original = original.Replace(value, value.Replace('-', '_'));
            return original;
        }
    }
}
