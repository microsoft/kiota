using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Microsoft.OpenApi.MicrosoftExtensions;

namespace Kiota.Builder.Extensions;

public static partial class OpenApiUrlTreeNodeExtensions
{
    private static string GetDotIfBothNotNullOfEmpty(string x, string y) => string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y) ? string.Empty : ".";
    private static readonly Func<string, string> replaceSingleParameterSegmentByItem =
    static x => x.IsPathSegmentWithSingleSimpleParameter() ? "item" : (ReservedItemName.Equals(x, StringComparison.OrdinalIgnoreCase) ? ReservedItemNameEscaped : x);
    private static readonly char[] namespaceNameSplitCharacters = ['.', '-', '$']; //$ref from OData
    private const string EscapedSuffix = "Escaped";
    internal const string ReservedItemName = "Item";
    internal const string ReservedItemNameEscaped = $"{ReservedItemName}_{EscapedSuffix}";
    internal static string GetNamespaceFromPath(this string currentPath, string prefix) =>
        prefix +
                ((currentPath?.Contains(PathNameSeparator, StringComparison.OrdinalIgnoreCase) ?? false) ?
                    (string.IsNullOrEmpty(prefix) ? string.Empty : ".")
                            + currentPath
                            .Split(PathNameSeparator, StringSplitOptions.RemoveEmptyEntries)
                            .Select(replaceSingleParameterSegmentByItem)
                            .Select(static x => string.Join(string.Empty, x
                                                    .Split(namespaceNameSplitCharacters, StringSplitOptions.RemoveEmptyEntries)
                                                    .Select(CleanupParametersFromPath)
                                                    .Select(static (y, idx) => idx == 0 ? y : y.ToFirstCharacterUpperCase())))
                            .Select(static x => SegmentsToSkipForClassNames.Contains(x) ? $"{x}{EscapedSuffix}" : x)
                            .Select(static x => x.CleanupSymbolName())
                            .Select(static x => GenerationConfiguration.ModelsNamespaceSegmentName.Equals(x, StringComparison.OrdinalIgnoreCase) ? $"{x}Requests" : x) //avoids projecting requests builders to models namespace
                            .Aggregate(string.Empty,
                                static (x, y) => $"{x}{GetDotIfBothNotNullOfEmpty(x, y)}{y}") :
                    string.Empty)
                .ReplaceValueIdentifier();
    public static string GetNodeNamespaceFromPath(this OpenApiUrlTreeNode currentNode, string prefix)
    {
        ArgumentNullException.ThrowIfNull(currentNode);
        return currentNode.Path.GetNamespaceFromPath(prefix);
    }
    //{id}, name(idParam={id}), name(idParam='{id}'), name(idParam='{id}',idParam2='{id2}')
    [GeneratedRegex(@"(?<prefix>\w+)?(?<equals>=?)'?\{(?<paramName>\w+)\}'?,?", RegexOptions.Singleline, 500)]
    private static partial Regex PathParametersRegex();
    // microsoft.graph.getRoleScopeTagsByIds(ids=@ids)
    [GeneratedRegex(@"=@(\w+)", RegexOptions.Singleline, 500)]
    private static partial Regex AtSignPathParameterRegex();
    private const char RequestParametersChar = '{';
    private const char RequestParametersEndChar = '}';
    private const string RequestParametersSectionChar = "(";
    private const string RequestParametersSectionEndChar = ")";
    private const string WithKeyword = "With";
    private static readonly MatchEvaluator requestParametersMatchEvaluator = match =>
        string.IsNullOrEmpty(match.Groups["equals"].Value) ?
            match.Groups["prefix"].Value + WithKeyword + match.Groups["paramName"].Value.ToFirstCharacterUpperCase() :
            WithKeyword + match.Groups["paramName"].Value.ToFirstCharacterUpperCase();
    internal static string CleanupParametersFromPath(string pathSegment)
    {
        if (string.IsNullOrEmpty(pathSegment))
            return pathSegment;
        return PathParametersRegex().Replace(
                                        AtSignPathParameterRegex().Replace(pathSegment, "={$1}"),
                                        requestParametersMatchEvaluator)
                                    .Replace(RequestParametersSectionEndChar, string.Empty, StringComparison.OrdinalIgnoreCase)
                                    .Replace(RequestParametersSectionChar, string.Empty, StringComparison.OrdinalIgnoreCase);
    }
    private static IEnumerable<IOpenApiParameter> GetParametersForPathItem(IOpenApiPathItem pathItem, string nodeSegment)
    {
        return (pathItem.Parameters ?? [])
                    .Union(pathItem.Operations?.SelectMany(static x => x.Value.Parameters ?? Enumerable.Empty<IOpenApiParameter>()) ?? [])
                    .Where(static x => x.In == ParameterLocation.Path)
                    .Where(x => nodeSegment.Contains($"{{{x.Name}}}", StringComparison.OrdinalIgnoreCase));
    }
    public static IEnumerable<IOpenApiParameter> GetPathParametersForCurrentSegment(this OpenApiUrlTreeNode node)
    {
        if (node != null &&
            node.IsComplexPathMultipleParameters())
            if (node.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pathItem))
                return GetParametersForPathItem(pathItem, node.DeduplicatedSegment());
            else if (node.Children.Count > 0)
                return node.Children
                            .Where(static x => x.Value.PathItems.ContainsKey(Constants.DefaultOpenApiLabel))
                            .SelectMany(x => GetParametersForPathItem(x.Value.PathItems[Constants.DefaultOpenApiLabel], node.DeduplicatedSegment()))
                            .Distinct();
        return [];
    }
    private const char PathNameSeparator = '\\';
    [GeneratedRegex(@"-?id\d?}?$", RegexOptions.Singleline | RegexOptions.IgnoreCase, 500)]
    private static partial Regex idClassNameCleanup();
    ///<summary>
    /// Returns the class name for the node with more or less precision depending on the provided arguments
    ///</summary>
    internal static string GetClassName(this OpenApiUrlTreeNode currentNode, StructuredMimeTypesCollection structuredMimeTypes, string? suffix = default, string? prefix = default, OpenApiOperation? operation = default, IOpenApiResponse? response = default, IOpenApiSchema? schema = default, bool requestBody = false)
    {
        ArgumentNullException.ThrowIfNull(currentNode);
        return currentNode.GetSegmentName(structuredMimeTypes, suffix, prefix, operation, response, schema, requestBody, static x => x.LastOrDefault() ?? string.Empty);
    }
    internal static string GetNavigationPropertyName(this OpenApiUrlTreeNode currentNode, StructuredMimeTypesCollection structuredMimeTypes, string? suffix = default, string? prefix = default, OpenApiOperation? operation = default, OpenApiResponse? response = default, OpenApiSchema? schema = default, bool requestBody = false, string? placeholder = null)
    {
        ArgumentNullException.ThrowIfNull(currentNode);
        var result = currentNode.GetSegmentName(structuredMimeTypes, suffix, prefix, operation, response, schema, requestBody, static x => string.Join(string.Empty, x.Select(static (y, idx) => idx == 0 ? y : y.ToFirstCharacterUpperCase())), false, placeholder);
        if (httpVerbs.Contains(result))
            return $"{result}Path"; // we don't run the change of an operation conflicting with a path on the same request builder
        return result;
    }
    private static readonly HashSet<string> httpVerbs = new(8, StringComparer.OrdinalIgnoreCase) { "get", "post", "put", "patch", "delete", "head", "options", "trace" };
    private static string GetSegmentName(this OpenApiUrlTreeNode currentNode, StructuredMimeTypesCollection structuredMimeTypes, string? suffix, string? prefix, OpenApiOperation? operation, IOpenApiResponse? response, IOpenApiSchema? schema, bool requestBody, Func<IEnumerable<string>, string> segmentsReducer, bool skipExtension = true, string? placeholder = null)
    {
        var referenceName = schema?.GetClassName();
        var rawClassName = referenceName is not null && !string.IsNullOrEmpty(referenceName) ?
                            referenceName :
                            ((requestBody ? null : response?.GetResponseSchema(structuredMimeTypes)?.GetClassName()) is string responseClassName && !string.IsNullOrEmpty(responseClassName) ?
                                responseClassName :
                                ((requestBody ? operation?.GetRequestSchema(structuredMimeTypes) : operation?.GetResponseSchema(structuredMimeTypes))?.GetClassName() is string requestClassName && !string.IsNullOrEmpty(requestClassName) ?
                                    requestClassName :
                                    CleanupParametersFromPath(currentNode.DeduplicatedSegment())?.ReplaceValueIdentifier()));
        if (string.IsNullOrEmpty(rawClassName) && !string.IsNullOrEmpty(placeholder))
            rawClassName = placeholder;
        if (!string.IsNullOrEmpty(rawClassName) && string.IsNullOrEmpty(referenceName))
        {
            if (stripExtensionForIndexersTestRegex().IsMatch(rawClassName)) // {id}.json is considered as indexer
                rawClassName = stripExtensionForIndexersRegex().Replace(rawClassName, string.Empty);
            if ((currentNode?.DoesNodeBelongToItemSubnamespace() ?? false) && idClassNameCleanup().Replace(rawClassName, string.Empty) is string cleanedUpClassName && !cleanedUpClassName.Equals(rawClassName, StringComparison.Ordinal))
            {
                rawClassName = cleanedUpClassName;
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
    [GeneratedRegex(@"[\r\n\t]", RegexOptions.Singleline, 500)]
    private static partial Regex descriptionCleanupRegex();
    public static string CleanupDescription(this string? description) => string.IsNullOrEmpty(description) ? string.Empty : descriptionCleanupRegex().Replace(description, string.Empty);
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
        currentNode?.DeduplicatedSegment().IsPathSegmentWithSingleSimpleParameter() ?? false;
    internal static bool IsPathSegmentWithSingleSimpleParameter(this string currentSegment)
    {
        if (string.IsNullOrEmpty(currentSegment)) return false;
        var segmentWithoutExtension = stripExtensionForIndexersRegex().Replace(currentSegment, string.Empty);

        return segmentWithoutExtension.StartsWith(RequestParametersChar) &&
                segmentWithoutExtension.EndsWith(RequestParametersEndChar) &&
                segmentWithoutExtension.IsPathSegmentWithNumberOfParameters(static x => x.Count() == 1);
    }
    private static bool IsPathSegmentWithNumberOfParameters(this string currentSegment, Func<IEnumerable<char>, bool> eval)
    {
        if (string.IsNullOrEmpty(currentSegment)) return false;

        return eval(currentSegment.Where(static x => x == RequestParametersChar));
    }
    [GeneratedRegex(@"\.(?:json|yaml|yml|csv|txt)$", RegexOptions.Singleline, 500)]
    private static partial Regex stripExtensionForIndexersRegex(); // so {param-name}.json is considered as indexer
    [GeneratedRegex(@"\{\w+\}\.(?:json|yaml|yml|csv|txt)$", RegexOptions.Singleline, 500)]
    private static partial Regex stripExtensionForIndexersTestRegex(); // so {param-name}.json is considered as indexer
    public static bool IsComplexPathMultipleParameters(this OpenApiUrlTreeNode currentNode) =>
        (currentNode?.DeduplicatedSegment()?.IsPathSegmentWithNumberOfParameters(static x => x.Any()) ?? false) && !currentNode.IsPathSegmentWithSingleSimpleParameter();

    public static bool HasRequiredQueryParametersAcrossOperations(this OpenApiUrlTreeNode currentNode)
    {
        ArgumentNullException.ThrowIfNull(currentNode);
        if (!currentNode.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pathItem))
            return false;

        var operationQueryParameters = pathItem.Operations?.SelectMany(static x => x.Value.Parameters ?? Enumerable.Empty<IOpenApiParameter>()) ?? [];
        return operationQueryParameters.Union(pathItem.Parameters ?? []).Where(static x => x.In == ParameterLocation.Query)
            .Any(static x => x.Required);
    }

    private static IOpenApiParameter[] GetParameters(this IOpenApiPathItem pathItem, HttpMethod? operationType = null)
    {
        var operationQueryParameters = (operationType, pathItem.Operations is { Count: > 0 }) switch
        {
            (HttpMethod ot, _) when pathItem.Operations!.TryGetValue(ot, out var operation) && operation.Parameters is not null => operation.Parameters,
            (null, true) => pathItem.Operations!.SelectMany(static x => x.Value.Parameters ?? Enumerable.Empty<IOpenApiParameter>()).Where(static x => x.In == ParameterLocation.Query),
            _ => [],
        };
        return (pathItem.Parameters ?? Enumerable.Empty<IOpenApiParameter>())
                        .Union(operationQueryParameters)
                        .Where(static x => x.In == ParameterLocation.Query)
                        .DistinctBy(static x => x.Name, StringComparer.Ordinal)
                        .OrderBy(static x => x.Name, StringComparer.Ordinal)
                        .ToArray() ?? [];
    }

    public static string GetUrlTemplate(this OpenApiUrlTreeNode currentNode, HttpMethod? operationType = null, bool includeQueryParameters = true, bool includeBaseUrl = true)
    {
        ArgumentNullException.ThrowIfNull(currentNode);
        var queryStringParameters = string.Empty;
        var trailingSlashItem = $"{currentNode.Path}\\";
        if (includeQueryParameters && currentNode.HasOperations(Constants.DefaultOpenApiLabel))
        {
            var pathItem = currentNode.PathItems[Constants.DefaultOpenApiLabel];

            var parameters = pathItem.GetParameters(operationType);
            if (parameters.Length != 0)
            {
                var requiredParameters = string.Join("&", parameters.Where(static x => x.Required)
                                                .Select(static x =>
                                                            $"{x.Name}={{{x.Name?.SanitizeParameterNameForUrlTemplate()}}}"));
                var optionalParameters = string.Join(",", parameters.Where(static x => !x.Required)
                                                .Select(static x =>
                                                            x.Name?.SanitizeParameterNameForUrlTemplate() +
                                                            (x.Explode ?
                                                                "*" : string.Empty)));
                var hasRequiredParameters = !string.IsNullOrEmpty(requiredParameters);
                var hasOptionalParameters = !string.IsNullOrEmpty(optionalParameters);
                queryStringParameters = $"{(hasRequiredParameters ? "?" : string.Empty)}{requiredParameters}{(hasOptionalParameters ? "{" : string.Empty)}{(hasOptionalParameters && hasRequiredParameters ? "&" : string.Empty)}{(hasOptionalParameters && !hasRequiredParameters ? "?" : string.Empty)}{optionalParameters}{(hasOptionalParameters ? "}" : string.Empty)}";
            }
        }
        var pathReservedPathParametersIds = currentNode.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pItem) ?
                                                pItem.Parameters?
                                                        .Union(pItem.Operations?.SelectMany(static x => x.Value.Parameters ?? Enumerable.Empty<IOpenApiParameter>()) ?? [])
                                                        .Where(static x => x.In == ParameterLocation.Path && x.Extensions is not null && x.Extensions.TryGetValue(OpenApiReservedParameterExtension.Name, out var ext) && ext is OpenApiReservedParameterExtension reserved && reserved.IsReserved.HasValue && reserved.IsReserved.Value)
                                                        .Select(static x => x.Name)
                                                        .OfType<string>()
                                                        .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [] :
                                                [];
        return (includeBaseUrl ? "{+baseurl}" : string.Empty) +
                SanitizePathParameterNamesForUrlTemplate(currentNode.Path.Replace('\\', '/'), pathReservedPathParametersIds) +
                queryStringParameters;
    }
    public static IEnumerable<KeyValuePair<string, HashSet<string>>> GetRequestInfo(this OpenApiUrlTreeNode currentNode)
    {
        ArgumentNullException.ThrowIfNull(currentNode);
        return currentNode.GetRequestInfoInternal();
    }
    private static IEnumerable<KeyValuePair<string, HashSet<string>>> GetRequestInfoInternal(this OpenApiUrlTreeNode currentNode)
    {
        foreach (var childInfo in currentNode.Children.Values.SelectMany(static x => x.GetRequestInfoInternal()))
        {
            yield return childInfo;
        }
        if (currentNode.PathItems
                            .Where(static x => x.Value.Operations is not null)
                            .SelectMany(static x => x.Value.Operations!)
                            .ToArray() is { Length: > 0 } operations)
        {
            yield return new KeyValuePair<string, HashSet<string>>(currentNode.GetUrlTemplate(null, false, false).TrimStart('/'), operations.Select(static x => x.Key.ToString().ToUpperInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase));
        }
    }
    [GeneratedRegex(@"{[^}]+}", RegexOptions.Singleline, 500)]
    private static partial Regex pathParamMatcher();
    private static string SanitizePathParameterNamesForUrlTemplate(string original, HashSet<string> reservedParameterNames)
    {
        if (string.IsNullOrEmpty(original) || !original.Contains('{', StringComparison.OrdinalIgnoreCase)) return original;
        var updated = original;
        foreach (var match in pathParamMatcher().EnumerateMatches(original))
        {
            var value = original[(match.Index + 1)..(match.Index + match.Length - 1)];// ignore the { and }
            updated = updated.Replace(value, (reservedParameterNames.Contains(value) ? "+" : string.Empty) + value.SanitizeParameterNameForUrlTemplate(), StringComparison.Ordinal);
        }
        return updated;
    }
    public static string SanitizeParameterNameForUrlTemplate(this string original)
    {
        if (string.IsNullOrEmpty(original)) return original;
        return Uri.EscapeDataString(stripExtensionForIndexersRegex()
                                        .Replace(original, string.Empty) // {param-name}.json becomes {param-name}
                                .TrimStart('{')
                                .TrimEnd('}'))
                    .Replace("-", "%2D", StringComparison.OrdinalIgnoreCase)
                    .Replace(".", "%2E", StringComparison.OrdinalIgnoreCase)
                    .Replace("~", "%7E", StringComparison.OrdinalIgnoreCase);// - . ~ are invalid uri template character but don't get encoded by Uri.EscapeDataString
    }
    public static string DeSanitizeUrlTemplateParameter(this string original)
    {
        if (string.IsNullOrEmpty(original)) return original;
        return Uri.UnescapeDataString(original.Replace("%2D", "-", StringComparison.OrdinalIgnoreCase)
                    .Replace("%2E", ".", StringComparison.OrdinalIgnoreCase)
                    .Replace("%7E", "~", StringComparison.OrdinalIgnoreCase));
    }
    [GeneratedRegex(@"%[0-9A-F]{2}", RegexOptions.Singleline, 500)]
    private static partial Regex removePctEncodedCharacters();
    public static string SanitizeParameterNameForCodeSymbols(this string original, string replaceEncodedCharactersWith = "")
    {
        if (string.IsNullOrEmpty(original)) return original;
        return removePctEncodedCharacters().Replace(original.ToOriginalCamelCase('-', '.', '~').SanitizeParameterNameForUrlTemplate(), replaceEncodedCharactersWith);
    }
    private const string DeduplicatedSegmentKey = "x-ms-kiota-deduplicatedSegment";
    public static string DeduplicatedSegment(this OpenApiUrlTreeNode currentNode)
    {
        if (currentNode is null) return string.Empty;
        if (currentNode.AdditionalData.TryGetValue(DeduplicatedSegmentKey, out var deduplicatedSegment) && deduplicatedSegment.FirstOrDefault() is string deduplicatedSegmentString)
            return deduplicatedSegmentString;
        return currentNode.Segment;
    }
    public static void AddDeduplicatedSegment(this OpenApiUrlTreeNode openApiUrlTreeNode, string newName)
    {
        ArgumentNullException.ThrowIfNull(openApiUrlTreeNode);
        ArgumentException.ThrowIfNullOrEmpty(newName);
        openApiUrlTreeNode.AdditionalData.Add(DeduplicatedSegmentKey, [newName]);
    }
    internal static void MergeIndexNodesAtSameLevel(this OpenApiUrlTreeNode node, ILogger logger)
    {
        var indexNodes = node.Children
                        .Where(static x => x.Value.IsPathSegmentWithSingleSimpleParameter())
                        .OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
        if (indexNodes.Length > 1)
        {
            var indexNode = indexNodes[0];
            node.Children.Remove(indexNode.Key);
            var oldSegmentName = indexNode.Value.Segment.Trim('{', '}').CleanupSymbolName();
            var segmentIndex = indexNode.Value.Path.Split('\\', StringSplitOptions.RemoveEmptyEntries).ToList().IndexOf(indexNode.Value.Segment);
            var newSegmentParameterName = oldSegmentName.EndsWith("-id", StringComparison.OrdinalIgnoreCase) ? oldSegmentName : $"{{{oldSegmentName.TrimSuffix("id", StringComparison.OrdinalIgnoreCase)}-id}}";
            indexNode.Value.Path = indexNode.Value.Path.Replace(indexNode.Key, newSegmentParameterName, StringComparison.OrdinalIgnoreCase);
            indexNode.Value.AdditionalData.Add(Constants.KiotaSegmentNameTreeNodeExtensionKey, [newSegmentParameterName]);
            indexNode.Value.AddDeduplicatedSegment(newSegmentParameterName);
            node.Children.Add(newSegmentParameterName, indexNode.Value);
            CopyNodeIntoOtherNode(indexNode.Value, indexNode.Value, indexNode.Key, newSegmentParameterName, logger);
            foreach (var child in indexNodes.Except([indexNode]))
            {
                node.Children.Remove(child.Key);
                CopyNodeIntoOtherNode(child.Value, indexNode.Value, child.Key, newSegmentParameterName, logger);
            }
            ReplaceParameterInPathForAllChildNodes(indexNode.Value, segmentIndex, newSegmentParameterName);
        }

        foreach (var child in node.Children.Values)
            MergeIndexNodesAtSameLevel(child, logger);
    }
    private static void ReplaceParameterInPathForAllChildNodes(OpenApiUrlTreeNode node, int parameterIndex, string newParameterName)
    {
        if (parameterIndex < 0)
            return;
        foreach (var child in node.Children.Values)
        {
            var splatPath = child.Path.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            if (splatPath.Length > parameterIndex)
            {
                var oldName = splatPath[parameterIndex];
                splatPath[parameterIndex] = newParameterName;
                child.Path = "\\" + string.Join('\\', splatPath);
                if (node.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pathItem) && pathItem.Parameters is not null)
                {
                    foreach (var pathParameter in pathItem.Parameters
                                                    .Union(pathItem.Operations?.SelectMany(static x => x.Value.Parameters ?? Enumerable.Empty<IOpenApiParameter>()) ?? [])
                                                    .Where(x => x.In == ParameterLocation.Path && oldName.Equals(x.Name, StringComparison.Ordinal)))
                    {
                        switch (pathParameter)
                        {
                            case OpenApiParameter openApiParameter:
                                openApiParameter.Name = newParameterName;
                                break;
                            case OpenApiParameterReference openApiReference when openApiReference.RecursiveTarget is { } openApiReferenceRecursiveTarget:
                                openApiReferenceRecursiveTarget.Name = newParameterName;
                                break;
                            default:
                                throw new InvalidOperationException("Unexpected parameter type");
                        }
                    }
                }
            }
            ReplaceParameterInPathForAllChildNodes(child, parameterIndex, newParameterName);
        }
    }
    private static void CopyNodeIntoOtherNode(OpenApiUrlTreeNode source, OpenApiUrlTreeNode destination, string pathParameterNameToReplace, string pathParameterNameReplacement, ILogger logger)
    {
        foreach (var child in source.Children)
        {
            child.Value.Path = child.Value.Path.Replace(pathParameterNameToReplace, pathParameterNameReplacement, StringComparison.OrdinalIgnoreCase);
            if (!destination.Children.TryAdd(child.Key, child.Value))
                CopyNodeIntoOtherNode(child.Value, destination.Children[child.Key], pathParameterNameToReplace, pathParameterNameReplacement, logger);
        }
        pathParameterNameToReplace = pathParameterNameToReplace.Trim('{', '}');
        pathParameterNameReplacement = pathParameterNameReplacement.Trim('{', '}');
        foreach (var pathItem in source.PathItems)
        {
            foreach (var pathParameter in (pathItem.Value.Parameters ?? Enumerable.Empty<IOpenApiParameter>())
                                        .Where(x => x.In == ParameterLocation.Path && pathParameterNameToReplace.Equals(x.Name, StringComparison.Ordinal))
                                        .Union(
                                            pathItem.Value.Operations is null ?
                                                [] :
                                                pathItem.Value.Operations
                                                .SelectMany(static x => x.Value.Parameters ?? Enumerable.Empty<IOpenApiParameter>())
                                                .Where(x => x.In == ParameterLocation.Path && pathParameterNameToReplace.Equals(x.Name, StringComparison.Ordinal))
                                        ))
            {
                switch (pathParameter)
                {
                    case OpenApiParameter openApiParameter:
                        openApiParameter.Name = pathParameterNameReplacement;
                        break;
                    case OpenApiParameterReference openApiReference when openApiReference.RecursiveTarget is { } openApiReferenceTarget:
                        openApiReferenceTarget.Name = pathParameterNameReplacement;
                        break;
                    default:
                        throw new InvalidOperationException("Unexpected parameter type");
                }
            }
            if (source != destination && !destination.PathItems.TryAdd(pathItem.Key, pathItem.Value) &&
                destination.PathItems.TryGetValue(pathItem.Key, out var dpi) && dpi is OpenApiPathItem destinationPathItem)
            {
                if (pathItem.Value.Operations is { Count: > 0 })
                {
                    destinationPathItem.Operations ??= new Dictionary<HttpMethod, OpenApiOperation>();
                    foreach (var operation in pathItem.Value.Operations)
                    {
                        if (!destinationPathItem.Operations.TryAdd(operation.Key, operation.Value))
                        {
                            LogDuplicateOperation(logger, operation.Key, pathItem.Key);
                        }
                    }
                }
                if (pathItem.Value.Parameters is { Count: > 0 })
                {
                    destinationPathItem.Parameters ??= new List<IOpenApiParameter>();
                    foreach (var pathParameter in pathItem.Value.Parameters)
                    {
                        destinationPathItem.Parameters.Add(pathParameter);
                    }
                }
                if (pathItem.Value.Extensions is { Count: > 0 })
                {
                    destinationPathItem.Extensions ??= new Dictionary<string, IOpenApiExtension>();
                    foreach (var extension in pathItem.Value.Extensions)
                    {
                        if (!destinationPathItem.Extensions.TryAdd(extension.Key, extension.Value))
                        {
                            LogDuplicateExtension(logger, extension.Key, pathItem.Key);
                        }
                    }
                }
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Duplicate operation {Operation} in path {Path}")]
    private static partial void LogDuplicateOperation(ILogger logger, HttpMethod operation, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Duplicate extension {Extension} in path {Path}")]
    private static partial void LogDuplicateExtension(ILogger logger, string extension, string path);
}
