using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace Kiota.Builder.Extensions;
public static class OpenApiUrlTreeNodeExtensions
{
    private static string GetDotIfBothNotNullOfEmpty(string x, string y) => string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y) ? string.Empty : ".";
    private static readonly Func<string, string> replaceSingleParameterSegmentByItem =
    static x => x.IsPathSegmentWithSingleSimpleParameter() ? "item" : x;
    private static readonly char[] namespaceNameSplitCharacters = new[] { '.', '-', '$' }; //$ref from OData
    public static string GetNamespaceFromPath(this string currentPath, string prefix) =>
        prefix +
                ((currentPath?.Contains(pathNameSeparator) ?? false) ?
                    (string.IsNullOrEmpty(prefix) ? string.Empty : ".")
                            + currentPath
                            ?.Split(pathNameSeparator, StringSplitOptions.RemoveEmptyEntries)
                            ?.Select(replaceSingleParameterSegmentByItem)
                            ?.Select(static x => string.Join(string.Empty, x
                                                    .Split(namespaceNameSplitCharacters, StringSplitOptions.RemoveEmptyEntries)
                                                    .Except(SegmentsToSkipForClassNames, StringComparer.OrdinalIgnoreCase)
                                                    .Select(CleanupParametersFromPath)
                                                    .Select(static (y, idx) => idx == 0 ? y : y.ToFirstCharacterUpperCase())))
                            ?.Select(static x => x.CleanupSymbolName())
                            ?.Aggregate(string.Empty,
                                static (x, y) => $"{x}{GetDotIfBothNotNullOfEmpty(x, y)}{y}") :
                    string.Empty)
                .ReplaceValueIdentifier();
    public static string GetNodeNamespaceFromPath(this OpenApiUrlTreeNode currentNode, string prefix)
    {
        ArgumentNullException.ThrowIfNull(currentNode);
        return currentNode.Path.GetNamespaceFromPath(prefix);
    }
    //{id}, name(idParam={id}), name(idParam='{id}'), name(idParam='{id}',idParam2='{id2}')
    private static readonly Regex PathParametersRegex = new(@"(?:\w+)?=?'?\{(?<paramName>\w+)\}'?,?", RegexOptions.Compiled, Constants.DefaultRegexTimeout);
    // microsoft.graph.getRoleScopeTagsByIds(ids=@ids)
    private static readonly Regex AtSignPathParameterRegex = new(@"=@(\w+)", RegexOptions.Compiled, Constants.DefaultRegexTimeout);
    private static readonly char requestParametersChar = '{';
    private static readonly char requestParametersEndChar = '}';
    private const string RequestParametersSectionChar = "(";
    private const string RequestParametersSectionEndChar = ")";
    private const string WithKeyword = "With";
    private static readonly MatchEvaluator requestParametersMatchEvaluator = match =>
        WithKeyword + match.Groups["paramName"].Value.ToFirstCharacterUpperCase();
    private static string CleanupParametersFromPath(string pathSegment)
    {
        if (string.IsNullOrEmpty(pathSegment))
            return pathSegment;
        return PathParametersRegex.Replace(
                                        AtSignPathParameterRegex.Replace(pathSegment, "={$1}"),
                                        requestParametersMatchEvaluator)
                                    .Replace(RequestParametersSectionEndChar, string.Empty)
                                    .Replace(RequestParametersSectionChar, string.Empty);
    }
    private static IEnumerable<OpenApiParameter> GetParametersForPathItem(OpenApiPathItem pathItem, string nodeSegment)
    {
        return pathItem.Parameters
                    .Union(pathItem.Operations.SelectMany(static x => x.Value.Parameters))
                    .Where(static x => x.In == ParameterLocation.Path)
                    .Where(x => nodeSegment.Contains($"{{{x.Name}}}", StringComparison.OrdinalIgnoreCase));
    }
    public static IEnumerable<OpenApiParameter> GetPathParametersForCurrentSegment(this OpenApiUrlTreeNode node)
    {
        if (node != null &&
            node.IsComplexPathMultipleParameters())
            if (node.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pathItem))
                return GetParametersForPathItem(pathItem, node.Segment);
            else if (node.Children.Any())
                return node.Children
                            .Where(static x => x.Value.PathItems.ContainsKey(Constants.DefaultOpenApiLabel))
                            .SelectMany(x => GetParametersForPathItem(x.Value.PathItems[Constants.DefaultOpenApiLabel], node.Segment))
                            .Distinct();
        return Enumerable.Empty<OpenApiParameter>();
    }
    private static readonly char pathNameSeparator = '\\';
    private static readonly Regex idClassNameCleanup = new(@"-?id\d?}?$", RegexOptions.Compiled | RegexOptions.IgnoreCase, Constants.DefaultRegexTimeout);
    ///<summary>
    /// Returns the class name for the node with more or less precision depending on the provided arguments
    ///</summary>
    public static string GetClassName(this OpenApiUrlTreeNode currentNode, HashSet<string> structuredMimeTypes, string? suffix = default, string? prefix = default, OpenApiOperation? operation = default, OpenApiResponse? response = default, OpenApiSchema? schema = default, bool requestBody = false)
    {
        return currentNode.GetSegmentName(structuredMimeTypes, suffix, prefix, operation, response, schema, requestBody, static x => x.LastOrDefault() ?? string.Empty);
    }
    public static string GetNavigationPropertyName(this OpenApiUrlTreeNode currentNode, HashSet<string> structuredMimeTypes, string? suffix = default, string? prefix = default, OpenApiOperation? operation = default, OpenApiResponse? response = default, OpenApiSchema? schema = default, bool requestBody = false)
    {
        var result = currentNode.GetSegmentName(structuredMimeTypes, suffix, prefix, operation, response, schema, requestBody, static x => string.Join(string.Empty, x.Select(static (y, idx) => idx == 0 ? y : y.ToFirstCharacterUpperCase())));
        if (httpVerbs.Contains(result))
            return $"{result}Path"; // we don't run the change of an operation conflicting with a path on the same request builder
        return result;
    }
    private static readonly HashSet<string> httpVerbs = new(StringComparer.OrdinalIgnoreCase) { "get", "post", "put", "patch", "delete", "head", "options", "trace" };
    private static string GetSegmentName(this OpenApiUrlTreeNode currentNode, HashSet<string> structuredMimeTypes, string? suffix, string? prefix, OpenApiOperation? operation, OpenApiResponse? response, OpenApiSchema? schema, bool requestBody, Func<IEnumerable<string>, string> segmentsReducer)
    {
        var rawClassName = schema?.Reference?.GetClassName() is string className && !string.IsNullOrEmpty(className) ?
                            className :
                            ((requestBody ? null : response?.GetResponseSchema(structuredMimeTypes)?.Reference?.GetClassName()) is string responseClassName && !string.IsNullOrEmpty(responseClassName) ?
                                responseClassName :
                                ((requestBody ? operation?.GetRequestSchema(structuredMimeTypes) : operation?.GetResponseSchema(structuredMimeTypes))?.Reference?.GetClassName() is string requestClassName && !string.IsNullOrEmpty(requestClassName) ?
                                    requestClassName :
                                    CleanupParametersFromPath(currentNode.Segment)?.ReplaceValueIdentifier()));
        if (!string.IsNullOrEmpty(rawClassName))
        {
            if (stripExtensionForIndexersRegex.IsMatch(rawClassName))
                rawClassName = stripExtensionForIndexersRegex.Replace(rawClassName, string.Empty);
            if ((currentNode?.DoesNodeBelongToItemSubnamespace() ?? false) && idClassNameCleanup.IsMatch(rawClassName))
            {
                rawClassName = idClassNameCleanup.Replace(rawClassName, string.Empty);
                if (WithKeyword.Equals(rawClassName, StringComparison.Ordinal)) // in case the single parameter doesn't follow {classname-id} we get the previous segment
                    rawClassName = currentNode.Path
                                            .Split(pathNameSeparator, StringSplitOptions.RemoveEmptyEntries)
                                            .SkipLast(1)
                                            .Last()
                                            .ToFirstCharacterUpperCase();
            }
        }

        var classNameSegments = rawClassName?.Split('.', StringSplitOptions.RemoveEmptyEntries).AsEnumerable() ?? Enumerable.Empty<string>();
        // only apply the exceptions if we had multiple segments.
        // Otherwise a single segment class name like `Json` will be returned as an empty string.
        if (classNameSegments.Count() > 1)
            classNameSegments = classNameSegments.Except(SegmentsToSkipForClassNames, StringComparer.OrdinalIgnoreCase);

        return (prefix + segmentsReducer(classNameSegments) + suffix).CleanupSymbolName();
    }
    private static readonly HashSet<string> SegmentsToSkipForClassNames = new(6, StringComparer.OrdinalIgnoreCase) {
        "json",
        "xml",
        "csv",
        "yaml",
        "yml",
        "txt",
    };
    private static readonly Regex descriptionCleanupRegex = new(@"[\r\n\t]", RegexOptions.Compiled, Constants.DefaultRegexTimeout);
    public static string CleanupDescription(this string? description) => string.IsNullOrEmpty(description) ? string.Empty : descriptionCleanupRegex.Replace(description, string.Empty);
    public static string GetPathItemDescription(this OpenApiUrlTreeNode currentNode, string label, string? defaultValue = default) =>
    !string.IsNullOrEmpty(label) && (currentNode?.PathItems.ContainsKey(label) ?? false) ?
            (currentNode.PathItems[label].Description is string description && !string.IsNullOrEmpty(description) ?
                description :
                (currentNode.PathItems[label].Summary is string summary && !string.IsNullOrEmpty(summary) ?
                    summary :
            defaultValue)).CleanupDescription() :
        (defaultValue ?? string.Empty);
    public static bool DoesNodeBelongToItemSubnamespace(this OpenApiUrlTreeNode currentNode) => currentNode.IsPathSegmentWithSingleSimpleParameter();
    public static bool IsPathSegmentWithSingleSimpleParameter(this OpenApiUrlTreeNode currentNode) =>
        currentNode?.Segment.IsPathSegmentWithSingleSimpleParameter() ?? false;
    private static bool IsPathSegmentWithSingleSimpleParameter(this string currentSegment)
    {
        if (string.IsNullOrEmpty(currentSegment)) return false;
        var segmentWithoutExtension = stripExtensionForIndexersRegex.Replace(currentSegment, string.Empty);

        return segmentWithoutExtension.StartsWith(requestParametersChar) &&
                segmentWithoutExtension.EndsWith(requestParametersEndChar) &&
                segmentWithoutExtension.IsPathSegmentWithNumberOfParameters(static x => x.Count() == 1);
    }
    private static bool IsPathSegmentWithNumberOfParameters(this string currentSegment, Func<IEnumerable<char>, bool> eval)
    {
        if (string.IsNullOrEmpty(currentSegment)) return false;

        return eval(currentSegment.Where(static x => x == requestParametersChar));
    }
    private static readonly Regex stripExtensionForIndexersRegex = new(@"\.(?:json|yaml|yml|csv|txt)$", RegexOptions.Compiled, Constants.DefaultRegexTimeout); // so {param-name}.json is considered as indexer
    public static bool IsComplexPathMultipleParameters(this OpenApiUrlTreeNode currentNode) =>
        (currentNode?.Segment?.IsPathSegmentWithNumberOfParameters(static x => x.Any()) ?? false) && !currentNode.IsPathSegmentWithSingleSimpleParameter();
    public static string GetUrlTemplate(this OpenApiUrlTreeNode currentNode)
    {
        var queryStringParameters = string.Empty;
        if (currentNode.HasOperations(Constants.DefaultOpenApiLabel))
        {
            var pathItem = currentNode.PathItems[Constants.DefaultOpenApiLabel];
            var parameters = pathItem.Parameters
                                    .Where(static x => x.In == ParameterLocation.Query)
                                    .Union(
                                        pathItem.Operations
                                                .SelectMany(static x => x.Value.Parameters)
                                                .Where(static x => x.In == ParameterLocation.Query))
                                    .ToArray();
            if (parameters.Any())
                queryStringParameters = "{?" +
                                        parameters.Select(static x =>
                                                            x.Name.SanitizeParameterNameForUrlTemplate() +
                                                            (x.Explode ?
                                                                "*" : string.Empty))
                                                .Aggregate(static (x, y) => $"{x},{y}") +
                                        '}';
        }
        return "{+baseurl}" +
                SanitizePathParameterNamesForUrlTemplate(currentNode.Path.Replace('\\', '/')) +
                queryStringParameters;
    }
    private static readonly Regex pathParamMatcher = new(@"{(?<paramname>[^}]+)}", RegexOptions.Compiled, Constants.DefaultRegexTimeout);
    private static string SanitizePathParameterNamesForUrlTemplate(string original)
    {
        if (string.IsNullOrEmpty(original) || !original.Contains('{')) return original;
        var parameters = pathParamMatcher.Matches(original);
        foreach (var value in parameters.Select(x => x.Groups["paramname"].Value))
            original = original.Replace(value, value.SanitizeParameterNameForUrlTemplate());
        return original;
    }
    public static string SanitizeParameterNameForUrlTemplate(this string original)
    {
        if (string.IsNullOrEmpty(original)) return original;
        return Uri.EscapeDataString(stripExtensionForIndexersRegex
                                        .Replace(original, string.Empty) // {param-name}.json becomes {param-name}
                                .TrimStart('{')
                                .TrimEnd('}'))
                    .Replace("-", "%2D")
                    .Replace(".", "%2E")
                    .Replace("~", "%7E");// - . ~ are invalid uri template character but don't get encoded by Uri.EscapeDataString
    }
    private static readonly Regex removePctEncodedCharacters = new(@"%[0-9A-F]{2}", RegexOptions.Compiled, Constants.DefaultRegexTimeout);
    public static string SanitizeParameterNameForCodeSymbols(this string original, string replaceEncodedCharactersWith = "")
    {
        if (string.IsNullOrEmpty(original)) return original;
        return removePctEncodedCharacters.Replace(original.ToCamelCase('-', '.', '~').SanitizeParameterNameForUrlTemplate(), replaceEncodedCharactersWith);
    }
}
