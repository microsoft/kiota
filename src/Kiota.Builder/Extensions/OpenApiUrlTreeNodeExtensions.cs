using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Kiota.Builder.Configuration;
using Microsoft.OpenApi.MicrosoftExtensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace Kiota.Builder.Extensions;
public static class OpenApiUrlTreeNodeExtensions
{
    private static string GetDotIfBothNotNullOfEmpty(string x, string y) => string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y) ? string.Empty : ".";
    private static readonly Func<string, string> replaceSingleParameterSegmentByItem =
    static x => x.IsPathSegmentWithSingleSimpleParameter() ? "item" : x;
    private static readonly char[] namespaceNameSplitCharacters = new[] { '.', '-', '$' }; //$ref from OData
    internal static string GetNamespaceFromPath(this string currentPath, string prefix) =>
        prefix +
                ((currentPath?.Contains(PathNameSeparator, StringComparison.OrdinalIgnoreCase) ?? false) ?
                    (string.IsNullOrEmpty(prefix) ? string.Empty : ".")
                            + currentPath
                            ?.Split(PathNameSeparator, StringSplitOptions.RemoveEmptyEntries)
                            ?.Select(replaceSingleParameterSegmentByItem)
                            ?.Select(static x => string.Join(string.Empty, x
                                                    .Split(namespaceNameSplitCharacters, StringSplitOptions.RemoveEmptyEntries)
                                                    .Except(SegmentsToSkipForClassNames, StringComparer.OrdinalIgnoreCase)
                                                    .Select(CleanupParametersFromPath)
                                                    .Select(static (y, idx) => idx == 0 ? y : y.ToFirstCharacterUpperCase())))
                            ?.Select(static x => x.CleanupSymbolName())
                            ?.Select(static x => GenerationConfiguration.ModelsNamespaceSegmentName.Equals(x, StringComparison.OrdinalIgnoreCase) ? $"{x}Requests" : x) //avoids projecting requests builders to models namespace
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
    private const char RequestParametersChar = '{';
    private const char RequestParametersEndChar = '}';
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
                                    .Replace(RequestParametersSectionEndChar, string.Empty, StringComparison.OrdinalIgnoreCase)
                                    .Replace(RequestParametersSectionChar, string.Empty, StringComparison.OrdinalIgnoreCase);
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
    private const char PathNameSeparator = '\\';
    private static readonly Regex idClassNameCleanup = new(@"-?id\d?}?$", RegexOptions.Compiled | RegexOptions.IgnoreCase, Constants.DefaultRegexTimeout);
    ///<summary>
    /// Returns the class name for the node with more or less precision depending on the provided arguments
    ///</summary>
    internal static string GetClassName(this OpenApiUrlTreeNode currentNode, StructuredMimeTypesCollection structuredMimeTypes, string? suffix = default, string? prefix = default, OpenApiOperation? operation = default, OpenApiResponse? response = default, OpenApiSchema? schema = default, bool requestBody = false)
    {
        ArgumentNullException.ThrowIfNull(currentNode);
        return currentNode.GetSegmentName(structuredMimeTypes, suffix, prefix, operation, response, schema, requestBody, static x => x.LastOrDefault() ?? string.Empty);
    }
    internal static string GetNavigationPropertyName(this OpenApiUrlTreeNode currentNode, StructuredMimeTypesCollection structuredMimeTypes, string? suffix = default, string? prefix = default, OpenApiOperation? operation = default, OpenApiResponse? response = default, OpenApiSchema? schema = default, bool requestBody = false)
    {
        ArgumentNullException.ThrowIfNull(currentNode);
        var result = currentNode.GetSegmentName(structuredMimeTypes, suffix, prefix, operation, response, schema, requestBody, static x => string.Join(string.Empty, x.Select(static (y, idx) => idx == 0 ? y : y.ToFirstCharacterUpperCase())), false);
        if (httpVerbs.Contains(result))
            return $"{result}Path"; // we don't run the change of an operation conflicting with a path on the same request builder
        return result;
    }
    private static readonly HashSet<string> httpVerbs = new(StringComparer.OrdinalIgnoreCase) { "get", "post", "put", "patch", "delete", "head", "options", "trace" };
    private static string GetSegmentName(this OpenApiUrlTreeNode currentNode, StructuredMimeTypesCollection structuredMimeTypes, string? suffix, string? prefix, OpenApiOperation? operation, OpenApiResponse? response, OpenApiSchema? schema, bool requestBody, Func<IEnumerable<string>, string> segmentsReducer, bool skipExtension = true)
    {
        var referenceName = schema?.Reference?.GetClassName();
        var rawClassName = referenceName is not null && !string.IsNullOrEmpty(referenceName) ?
                            referenceName :
                            ((requestBody ? null : response?.GetResponseSchema(structuredMimeTypes)?.Reference?.GetClassName()) is string responseClassName && !string.IsNullOrEmpty(responseClassName) ?
                                responseClassName :
                                ((requestBody ? operation?.GetRequestSchema(structuredMimeTypes) : operation?.GetResponseSchema(structuredMimeTypes))?.Reference?.GetClassName() is string requestClassName && !string.IsNullOrEmpty(requestClassName) ?
                                    requestClassName :
                                    CleanupParametersFromPath(currentNode.Segment)?.ReplaceValueIdentifier()));
        if (!string.IsNullOrEmpty(rawClassName) && string.IsNullOrEmpty(referenceName))
        {
            if (stripExtensionForIndexersTestRegex.IsMatch(rawClassName))
                rawClassName = stripExtensionForIndexersRegex.Replace(rawClassName, string.Empty);
            if ((currentNode?.DoesNodeBelongToItemSubnamespace() ?? false) && idClassNameCleanup.IsMatch(rawClassName))
            {
                rawClassName = idClassNameCleanup.Replace(rawClassName, string.Empty);
                if (WithKeyword.Equals(rawClassName, StringComparison.Ordinal)) // in case the single parameter doesn't follow {classname-id} we get the previous segment
                    rawClassName = currentNode.Path
                                            .Split(PathNameSeparator, StringSplitOptions.RemoveEmptyEntries)
                                            .SkipLast(1)
                                            .Last()
                                            .ToFirstCharacterUpperCase();
            }
        }

        var classNameSegments = rawClassName?.Split('.', StringSplitOptions.RemoveEmptyEntries).AsEnumerable() ?? Enumerable.Empty<string>();
        // only apply the exceptions if we had multiple segments.
        // Otherwise a single segment class name like `Json` will be returned as an empty string.
        if (skipExtension && classNameSegments.Count() > 1)
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
    public static string GetPathItemDescription(this OpenApiUrlTreeNode currentNode, string label, string? defaultValue = default)
    {
        if (currentNode != null && !string.IsNullOrEmpty(label) && currentNode.PathItems.TryGetValue(label, out var pathItem))
            return ((string.IsNullOrEmpty(pathItem.Description), string.IsNullOrEmpty(pathItem.Summary)) switch
            {
                (false, _) => pathItem.Description,
                (_, false) => pathItem.Summary,
                (_, _) => defaultValue,
            }).CleanupDescription();
        return string.IsNullOrEmpty(defaultValue) ? string.Empty : defaultValue;
    }
    public static bool DoesNodeBelongToItemSubnamespace(this OpenApiUrlTreeNode currentNode) => currentNode.IsPathSegmentWithSingleSimpleParameter();
    public static bool IsPathSegmentWithSingleSimpleParameter(this OpenApiUrlTreeNode currentNode) =>
        currentNode?.Segment.IsPathSegmentWithSingleSimpleParameter() ?? false;
    private static bool IsPathSegmentWithSingleSimpleParameter(this string currentSegment)
    {
        if (string.IsNullOrEmpty(currentSegment)) return false;
        var segmentWithoutExtension = stripExtensionForIndexersRegex.Replace(currentSegment, string.Empty);

        return segmentWithoutExtension.StartsWith(RequestParametersChar) &&
                segmentWithoutExtension.EndsWith(RequestParametersEndChar) &&
                segmentWithoutExtension.IsPathSegmentWithNumberOfParameters(static x => x.Count() == 1);
    }
    private static bool IsPathSegmentWithNumberOfParameters(this string currentSegment, Func<IEnumerable<char>, bool> eval)
    {
        if (string.IsNullOrEmpty(currentSegment)) return false;

        return eval(currentSegment.Where(static x => x == RequestParametersChar));
    }
    private static readonly Regex stripExtensionForIndexersRegex = new(@"\.(?:json|yaml|yml|csv|txt)$", RegexOptions.Compiled, Constants.DefaultRegexTimeout); // so {param-name}.json is considered as indexer
    private static readonly Regex stripExtensionForIndexersTestRegex = new(@"\{\w+\}\.(?:json|yaml|yml|csv|txt)$", RegexOptions.Compiled, Constants.DefaultRegexTimeout); // so {param-name}.json is considered as indexer
    public static bool IsComplexPathMultipleParameters(this OpenApiUrlTreeNode currentNode) =>
        (currentNode?.Segment?.IsPathSegmentWithNumberOfParameters(static x => x.Any()) ?? false) && !currentNode.IsPathSegmentWithSingleSimpleParameter();
    public static string GetUrlTemplate(this OpenApiUrlTreeNode currentNode)
    {
        ArgumentNullException.ThrowIfNull(currentNode);
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
                                    .DistinctBy(static x => x.Name)
                                    .ToArray();
            if (parameters.Length != 0)
                queryStringParameters = "{?" +
                                        parameters.Select(static x =>
                                                            x.Name.SanitizeParameterNameForUrlTemplate() +
                                                            (x.Explode ?
                                                                "*" : string.Empty))
                                                .Aggregate(static (x, y) => $"{x},{y}") +
                                        '}';
        }
        var pathReservedPathParametersIds = currentNode.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pItem) ?
                                                pItem.Parameters
                                                        .Union(pItem.Operations.SelectMany(static x => x.Value.Parameters))
                                                        .Where(static x => x.In == ParameterLocation.Path && x.Extensions.TryGetValue(OpenApiReservedParameterExtension.Name, out var ext) && ext is OpenApiReservedParameterExtension reserved && reserved.IsReserved.HasValue && reserved.IsReserved.Value)
                                                        .Select(static x => x.Name)
                                                        .ToHashSet(StringComparer.OrdinalIgnoreCase) :
                                                new HashSet<string>();
        return "{+baseurl}" +
                SanitizePathParameterNamesForUrlTemplate(currentNode.Path.Replace('\\', '/'), pathReservedPathParametersIds) +
                queryStringParameters;
    }
    private static readonly Regex pathParamMatcher = new(@"{(?<paramname>[^}]+)}", RegexOptions.Compiled, Constants.DefaultRegexTimeout);
    private static string SanitizePathParameterNamesForUrlTemplate(string original, HashSet<string> reservedParameterNames)
    {
        if (string.IsNullOrEmpty(original) || !original.Contains('{', StringComparison.OrdinalIgnoreCase)) return original;
        var parameters = pathParamMatcher.Matches(original);
        foreach (var value in parameters.Select(x => x.Groups["paramname"].Value))
            original = original.Replace(value, (reservedParameterNames.Contains(value) ? "+" : string.Empty) + value.SanitizeParameterNameForUrlTemplate(), StringComparison.Ordinal);
        return original;
    }
    public static string SanitizeParameterNameForUrlTemplate(this string original)
    {
        if (string.IsNullOrEmpty(original)) return original;
        return Uri.EscapeDataString(stripExtensionForIndexersRegex
                                        .Replace(original, string.Empty) // {param-name}.json becomes {param-name}
                                .TrimStart('{')
                                .TrimEnd('}'))
                    .Replace("-", "%2D", StringComparison.OrdinalIgnoreCase)
                    .Replace(".", "%2E", StringComparison.OrdinalIgnoreCase)
                    .Replace("~", "%7E", StringComparison.OrdinalIgnoreCase);// - . ~ are invalid uri template character but don't get encoded by Uri.EscapeDataString
    }
    private static readonly Regex removePctEncodedCharacters = new(@"%[0-9A-F]{2}", RegexOptions.Compiled, Constants.DefaultRegexTimeout);
    public static string SanitizeParameterNameForCodeSymbols(this string original, string replaceEncodedCharactersWith = "")
    {
        if (string.IsNullOrEmpty(original)) return original;
        return removePctEncodedCharacters.Replace(original.ToCamelCase('-', '.', '~').SanitizeParameterNameForUrlTemplate(), replaceEncodedCharactersWith);
    }
}
