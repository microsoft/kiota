using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Kiota.Builder.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.MicrosoftExtensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Services;

namespace Kiota.Builder.Extensions;
public static partial class OpenApiUrlTreeNodeExtensions
{
    private static string GetDotIfBothNotNullOfEmpty(string x, string y) => string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y) ? string.Empty : ".";
    private static readonly Func<string, string> replaceSingleParameterSegmentByItem =
    static x => x.IsPathSegmentWithSingleSimpleParameter() ? "item" : x;
    private static readonly char[] namespaceNameSplitCharacters = ['.', '-', '$']; //$ref from OData
    private const string EscapedSuffix = "Escaped";
    internal static string GetNamespaceFromPath(this string currentPath, string prefix)
    {
        if (currentPath == null || !currentPath.Contains(PathNameSeparator, StringComparison.OrdinalIgnoreCase))
        {
            return prefix;
        }

        var segments = currentPath.Split(PathNameSeparator, StringSplitOptions.RemoveEmptyEntries);
        var transformedSegments = new List<string>();

        foreach (var segment in segments)
        {
            var replacedSegment = replaceSingleParameterSegmentByItem(segment);
            var subSegments = replacedSegment.Split(namespaceNameSplitCharacters, StringSplitOptions.RemoveEmptyEntries);
            var transformedSubSegments = new List<string>();

            for (int i = 0; i < subSegments.Length; i++)
            {
                var subSegment = subSegments[i];
                var cleanedSubSegment = CleanupParametersFromPath(subSegment);
                var transformedSubSegment = i == 0 ? cleanedSubSegment : cleanedSubSegment.ToFirstCharacterUpperCase();
                transformedSubSegments.Add(transformedSubSegment);
            }

            var transformedSegment = string.Join(string.Empty, transformedSubSegments);
            if (SegmentsToSkipForClassNames.Contains(transformedSegment))
            {
                transformedSegment += EscapedSuffix;
            }

            transformedSegment = transformedSegment.CleanupSymbolName();

            if (GenerationConfiguration.ModelsNamespaceSegmentName.Equals(transformedSegment, StringComparison.OrdinalIgnoreCase))
            {
                transformedSegment = $"{transformedSegment}Requests";
            }

            transformedSegments.Add(transformedSegment);
        }

        var aggregatedSegments = string.Empty;
        foreach (var segment in transformedSegments)
        {
            aggregatedSegments += GetDotIfBothNotNullOfEmpty(aggregatedSegments, segment) + segment;
        }

        var result = prefix + (string.IsNullOrEmpty(prefix) ? string.Empty : ".") + aggregatedSegments.ReplaceValueIdentifier();

        return result;
    }
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
    private static List<OpenApiParameter> GetParametersForPathItem(OpenApiPathItem pathItem, string nodeSegment)
    {
        var parameters = new List<OpenApiParameter>();

        // Add pathItem.Parameters to the list
        foreach (var parameter in pathItem.Parameters)
        {
            parameters.Add(parameter);
        }

        // Add parameters from each operation in pathItem.Operations to the list
        foreach (var operation in pathItem.Operations)
        {
            foreach (var parameter in operation.Value.Parameters)
            {
                parameters.Add(parameter);
            }
        }

        // Filter the list to only include parameters where In == ParameterLocation.Path
        var pathParameters = new List<OpenApiParameter>();
        foreach (var parameter in parameters)
        {
            if (parameter.In == ParameterLocation.Path)
            {
                pathParameters.Add(parameter);
            }
        }

        // Filter the list to only include parameters where nodeSegment contains the parameter name
        var filteredParameters = new List<OpenApiParameter>();
        foreach (var parameter in pathParameters)
        {
            if (nodeSegment.Contains($"{{{parameter.Name}}}", StringComparison.OrdinalIgnoreCase))
            {
                filteredParameters.Add(parameter);
            }
        }

        return filteredParameters;
    }
    public static IEnumerable<OpenApiParameter> GetPathParametersForCurrentSegment(this OpenApiUrlTreeNode node)
    {
        var parameters = new List<OpenApiParameter>();

        if (node != null && node.IsComplexPathMultipleParameters())
        {
            if (node.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pathItem))
            {
                parameters.AddRange(GetParametersForPathItem(pathItem, node.DeduplicatedSegment()));
            }
            else
            {
                foreach (var child in node.Children)
                {
                    if (child.Value.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var value))
                    {
                        parameters.AddRange(GetParametersForPathItem(value, node.DeduplicatedSegment()));
                    }
                }
            }

            // Remove duplicates
            var distinctParameters = new List<OpenApiParameter>();
            foreach (var parameter in parameters)
            {
                if (!distinctParameters.Contains(parameter))
                {
                    distinctParameters.Add(parameter);
                }
            }

            return distinctParameters;
        }

        return [];
    }

    private const char PathNameSeparator = '\\';

    [GeneratedRegex(@"-?id\d?}?$", RegexOptions.Singleline | RegexOptions.IgnoreCase, 500)]
    private static partial Regex idClassNameCleanup();

    ///<summary>
    /// Returns the class name for the node with more or less precision depending on the provided arguments
    ///</summary>
    internal static string GetClassName(this OpenApiUrlTreeNode currentNode, StructuredMimeTypesCollection structuredMimeTypes, string? suffix = default, string? prefix = default, OpenApiOperation? operation = default, OpenApiResponse? response = default, OpenApiSchema? schema = default, bool requestBody = false)
    {
        ArgumentNullException.ThrowIfNull(currentNode);
        return currentNode.GetSegmentName(structuredMimeTypes, suffix, prefix, operation, response, schema, requestBody, x =>
        {
            string lastElement = string.Empty;
            foreach (var element in x)
            {
                lastElement = element;
            }
            return lastElement;
        });
    }

    internal static string GetNavigationPropertyName(this OpenApiUrlTreeNode currentNode, StructuredMimeTypesCollection structuredMimeTypes, string? suffix = default, string? prefix = default, OpenApiOperation? operation = default, OpenApiResponse? response = default, OpenApiSchema? schema = default, bool requestBody = false)
    {
        ArgumentNullException.ThrowIfNull(currentNode);
        var result = currentNode.GetSegmentName(structuredMimeTypes, suffix, prefix, operation, response, schema, requestBody, x =>
        {
            var transformedElements = new List<string>();
            int idx = 0;
            foreach (var y in x)
            {
                transformedElements.Add(idx == 0 ? y : y.ToFirstCharacterUpperCase());
                idx++;
            }
            return string.Join(string.Empty, transformedElements);
        }, false);

        bool isHttpVerb = false;
        foreach (var verb in httpVerbs)
        {
            if (verb == result)
            {
                isHttpVerb = true;
                break;
            }
        }

        if (isHttpVerb)
        {
            return $"{result}Path"; // we don't run the change of an operation conflicting with a path on the same request builder
        }
        return result;
    }

    private static readonly HashSet<string> httpVerbs = new(8, StringComparer.OrdinalIgnoreCase) { "get", "post", "put", "patch", "delete", "head", "options", "trace" };

    private static string GetSegmentName(this OpenApiUrlTreeNode currentNode, StructuredMimeTypesCollection structuredMimeTypes, string? suffix, string? prefix, OpenApiOperation? operation, OpenApiResponse? response, OpenApiSchema? schema, bool requestBody, Func<IEnumerable<string>, string> segmentsReducer, bool skipExtension = true)
    {
        var referenceName = schema?.Reference?.GetClassName();
        var rawClassName = referenceName is not null && !string.IsNullOrEmpty(referenceName) ?
                            referenceName :
                            ((requestBody ? null : response?.GetResponseSchema(structuredMimeTypes)?.Reference?.GetClassName()) is string responseClassName && !string.IsNullOrEmpty(responseClassName) ?
                                responseClassName :
                                ((requestBody ? operation?.GetRequestSchema(structuredMimeTypes) : operation?.GetResponseSchema(structuredMimeTypes))?.Reference?.GetClassName() is string requestClassName && !string.IsNullOrEmpty(requestClassName) ?
                                    requestClassName :
                                    CleanupParametersFromPath(currentNode.DeduplicatedSegment())?.ReplaceValueIdentifier()));
        if (!string.IsNullOrEmpty(rawClassName) && string.IsNullOrEmpty(referenceName))
        {
            if (stripExtensionForIndexersTestRegex().IsMatch(rawClassName)) // {id}.json is considered as indexer
                rawClassName = stripExtensionForIndexersRegex().Replace(rawClassName, string.Empty);
            if ((currentNode?.DoesNodeBelongToItemSubnamespace() ?? false) && idClassNameCleanup().Replace(rawClassName, string.Empty) is string cleanedUpClassName && !cleanedUpClassName.Equals(rawClassName, StringComparison.Ordinal))
            {
                rawClassName = cleanedUpClassName;
                if (WithKeyword.Equals(rawClassName, StringComparison.Ordinal)) // in case the single parameter doesn't follow {classname-id} we get the previous segment
                {
                    var pathSegments = currentNode.Path.Split(PathNameSeparator, StringSplitOptions.RemoveEmptyEntries);
                    rawClassName = pathSegments.Length > 1 ? pathSegments[pathSegments.Length - 2].ToFirstCharacterUpperCase() : string.Empty;
                }
            }
        }

        var classNameSegmentsArray = rawClassName?.Split('.', StringSplitOptions.RemoveEmptyEntries) ?? [];
        IEnumerable<string> classNameSegments = classNameSegmentsArray;
        // only apply the exceptions if we had multiple segments.
        // Otherwise a single segment class name like `Json` will be returned as an empty string.
        if (skipExtension && classNameSegmentsArray.Length > 1)
        {
            classNameSegments = FilterClasses(classNameSegmentsArray);
        }

        return (prefix + segmentsReducer(classNameSegments) + suffix).CleanupSymbolName();

        IEnumerable<string> FilterClasses(string[] segments)
        {
            foreach (var segment in segments)
            {
                bool isSkipSegment = false;
                foreach (var skipSegment in SegmentsToSkipForClassNames)
                {
                    if (string.Equals(segment, skipSegment, StringComparison.OrdinalIgnoreCase))
                    {
                        isSkipSegment = true;
                        break;
                    }
                }
                if (!isSkipSegment)
                {
                    yield return segment;
                }
            }
        }
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
                segmentWithoutExtension.IsPathSegmentWithNumberOfParameters(static x => 
                {
                    int count = 0;
                    foreach (var _ in x) count++;
                    return count == 1;
                });
    }

    private static bool IsPathSegmentWithNumberOfParameters(this string currentSegment, Func<IEnumerable<char>, bool> eval)
    {
        if (string.IsNullOrEmpty(currentSegment)) return false;

        var filteredChars = new List<char>();
        foreach (var c in currentSegment)
        {
            if (c == RequestParametersChar)
            {
                filteredChars.Add(c);
            }
        }

        return eval(filteredChars);
    }

    [GeneratedRegex(@"\.(?:json|yaml|yml|csv|txt)$", RegexOptions.Singleline, 500)]
    private static partial Regex stripExtensionForIndexersRegex(); // so {param-name}.json is considered as indexer

    [GeneratedRegex(@"\{\w+\}\.(?:json|yaml|yml|csv|txt)$", RegexOptions.Singleline, 500)]
    private static partial Regex stripExtensionForIndexersTestRegex(); // so {param-name}.json is considered as indexer
    
    public static bool IsComplexPathMultipleParameters(this OpenApiUrlTreeNode currentNode) =>
        (currentNode?.DeduplicatedSegment()?.IsPathSegmentWithNumberOfParameters(static x => 
        {
            foreach (var _ in x) return true;
            return false;
        }) ?? false) && !currentNode.IsPathSegmentWithSingleSimpleParameter();

    public static bool HasRequiredQueryParametersAcrossOperations(this OpenApiUrlTreeNode currentNode)
    {
        ArgumentNullException.ThrowIfNull(currentNode);
        if (!currentNode.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pathItem))
            return false;

        var operationQueryParameters = new List<OpenApiParameter>();
        foreach (var operation in pathItem.Operations)
        {
            operationQueryParameters.AddRange(operation.Value.Parameters);
        }

        operationQueryParameters.AddRange(pathItem.Parameters);

        foreach (var parameter in operationQueryParameters)
        {
            if (parameter.In == ParameterLocation.Query && parameter.Required)
            {
                return true;
            }
        }

        return false;
    }

    public static string GetUrlTemplate(this OpenApiUrlTreeNode currentNode, OperationType? operationType = null, bool includeQueryParameters = true, bool includeBaseUrl = true)
    {
        ArgumentNullException.ThrowIfNull(currentNode);
        var queryStringParameters = string.Empty;
        if (currentNode.HasOperations(Constants.DefaultOpenApiLabel) && includeQueryParameters)
        {
            var pathItem = currentNode.PathItems[Constants.DefaultOpenApiLabel];
            var operationQueryParameters = new List<OpenApiParameter>();
            if (operationType.HasValue && pathItem.Operations.TryGetValue(operationType.Value, out var operation))
            {
                operationQueryParameters.AddRange(operation.Parameters);
            }
            else if (pathItem.Operations.Count > 0)
            {
                foreach (var op in pathItem.Operations)
                {
                    operationQueryParameters.AddRange(op.Value.Parameters);
                }
            }

            var parameters = new List<OpenApiParameter>(pathItem.Parameters);
            parameters.AddRange(operationQueryParameters);
            parameters = DistinctBy(parameters, x => x.Name, StringComparer.Ordinal);
            parameters.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

            if (parameters.Count != 0)
            {
                var requiredParameters = new List<string>();
                var optionalParameters = new List<string>();
                foreach (var parameter in parameters)
                {
                    if (parameter.Required)
                    {
                        requiredParameters.Add($"{parameter.Name}={{{parameter.Name.SanitizeParameterNameForUrlTemplate()}}}");
                    }
                    else
                    {
                        optionalParameters.Add(parameter.Name.SanitizeParameterNameForUrlTemplate() + (parameter.Explode ? "*" : string.Empty));
                    }
                }

                var hasRequiredParameters = requiredParameters.Count > 0;
                var hasOptionalParameters = optionalParameters.Count > 0;
                queryStringParameters = $"{(hasRequiredParameters ? "?" : string.Empty)}{string.Join("&", requiredParameters)}{(hasOptionalParameters ? "{" : string.Empty)}{(hasOptionalParameters && hasRequiredParameters ? "&" : string.Empty)}{(hasOptionalParameters && !hasRequiredParameters ? "?" : string.Empty)}{string.Join(",", optionalParameters)}{(hasOptionalParameters ? "}" : string.Empty)}";
            }
        }

        var pathReservedPathParametersIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (currentNode.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pItem))
        {
            var parameters = new List<OpenApiParameter>(pItem.Parameters);
            foreach (var op in pItem.Operations)
            {
                parameters.AddRange(op.Value.Parameters);
            }

            foreach (var parameter in parameters)
            {
                if (parameter.In == ParameterLocation.Path && parameter.Extensions.TryGetValue(OpenApiReservedParameterExtension.Name, out var ext) && ext is OpenApiReservedParameterExtension reserved && reserved.IsReserved.HasValue && reserved.IsReserved.Value)
                {
                    pathReservedPathParametersIds.Add(parameter.Name);
                }
            }
        }

        return (includeBaseUrl ? "{+baseurl}" : string.Empty) +
                SanitizePathParameterNamesForUrlTemplate(currentNode.Path.Replace('\\', '/'), pathReservedPathParametersIds) +
                queryStringParameters;

        static List<T> DistinctBy<T, TKey>(List<T> list, Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            var seenKeys = new HashSet<TKey>(comparer);
            var newList = new List<T>();

            foreach (var element in list)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    newList.Add(element);
                }
            }

            return newList;
        }
    }

    public static IEnumerable<KeyValuePair<string, HashSet<string>>> GetRequestInfo(this OpenApiUrlTreeNode currentNode)
    {
        ArgumentNullException.ThrowIfNull(currentNode);
        return currentNode.GetRequestInfoInternal();
    }

    private static IEnumerable<KeyValuePair<string, HashSet<string>>> GetRequestInfoInternal(this OpenApiUrlTreeNode currentNode)
    {
        foreach (var child in currentNode.Children.Values)
        {
            foreach (var childInfo in GetRequestInfoInternal(child))
            {
                yield return childInfo;
            }
        }

        var operations = new List<KeyValuePair<OperationType, OpenApiOperation>>();
        foreach (var pathItem in currentNode.PathItems.Values)
        {
            operations.AddRange(pathItem.Operations);
        }

        if (operations.Count > 0)
        {
            var operationTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var operation in operations)
            {
                operationTypes.Add(operation.Key.ToString().ToUpperInvariant());
            }

            yield return new KeyValuePair<string, HashSet<string>>(currentNode.GetUrlTemplate(null, false, false).TrimStart('/'), operationTypes);
        }
    }

    [GeneratedRegex(@"{(?<paramname>[^}]+)}", RegexOptions.Singleline, 500)]
    private static partial Regex pathParamMatcher();

    private static string SanitizePathParameterNamesForUrlTemplate(string original, HashSet<string> reservedParameterNames)
    {
        if (string.IsNullOrEmpty(original) || !original.Contains('{', StringComparison.OrdinalIgnoreCase)) return original;
        var parameters = pathParamMatcher().Matches(original);
        foreach (Match match in parameters)
        {
            var value = match.Groups["paramname"].Value;
            original = original.Replace(value, (reservedParameterNames.Contains(value) ? "+" : string.Empty) + value.SanitizeParameterNameForUrlTemplate(), StringComparison.Ordinal);
        }
        return original;
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
        return removePctEncodedCharacters().Replace(original.ToCamelCase('-', '.', '~').SanitizeParameterNameForUrlTemplate(), replaceEncodedCharactersWith);
    }

    private const string DeduplicatedSegmentKey = "x-ms-kiota-deduplicatedSegment";

    public static string DeduplicatedSegment(this OpenApiUrlTreeNode currentNode)
    {
        if (currentNode is null) return string.Empty;
        if (currentNode.AdditionalData.TryGetValue(DeduplicatedSegmentKey, out var deduplicatedSegment) && deduplicatedSegment.Count > 0 && deduplicatedSegment[0] is string deduplicatedSegmentString)
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
        var indexNodes = new SortedList<string, OpenApiUrlTreeNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var child in node.Children)
        {
            if (child.Value.IsPathSegmentWithSingleSimpleParameter())
            {
                indexNodes.Add(child.Key, child.Value);
            }
        }

        if (indexNodes.Count > 1)
        {
            var indexNodeKey = indexNodes.Keys[0];
            var indexNodeValue = indexNodes.Values[0];
            node.Children.Remove(indexNodeKey);
            var oldSegmentName = indexNodeValue.Segment.Trim('{', '}').CleanupSymbolName();
            var segmentIndex = Array.IndexOf(indexNodeValue.Path.Split('\\', StringSplitOptions.RemoveEmptyEntries), indexNodeValue.Segment);
            var newSegmentParameterName = oldSegmentName.EndsWith("-id", StringComparison.OrdinalIgnoreCase) ? oldSegmentName : $"{{{oldSegmentName.TrimSuffix("id", StringComparison.OrdinalIgnoreCase)}-id}}";
            indexNodeValue.Path = indexNodeValue.Path.Replace(indexNodeKey, newSegmentParameterName, StringComparison.OrdinalIgnoreCase);
            indexNodeValue.AddDeduplicatedSegment(newSegmentParameterName);
            node.Children.Add(newSegmentParameterName, indexNodeValue);
            CopyNodeIntoOtherNode(indexNodeValue, indexNodeValue, indexNodeKey, newSegmentParameterName, logger);

            for (int i = 1; i < indexNodes.Count; i++)
            {
                var childKey = indexNodes.Keys[i];
                var childValue = indexNodes.Values[i];
                node.Children.Remove(childKey);
                CopyNodeIntoOtherNode(childValue, indexNodeValue, childKey, newSegmentParameterName, logger);
            }

            ReplaceParameterInPathForAllChildNodes(indexNodeValue, segmentIndex, newSegmentParameterName);
        }

        foreach (var child in node.Children.Values)
        {
            MergeIndexNodesAtSameLevel(child, logger);
        }
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
                if (node.PathItems.TryGetValue(Constants.DefaultOpenApiLabel, out var pathItem))
                {
                    foreach (var pathParameter in pathItem.Parameters)
                    {
                        if (pathParameter.In == ParameterLocation.Path && oldName.Equals(pathParameter.Name, StringComparison.Ordinal))
                        {
                            pathParameter.Name = newParameterName;
                        }
                    }

                    foreach (var operation in pathItem.Operations.Values)
                    {
                        foreach (var operationParameter in operation.Parameters)
                        {
                            if (operationParameter.In == ParameterLocation.Path && oldName.Equals(operationParameter.Name, StringComparison.Ordinal))
                            {
                                operationParameter.Name = newParameterName;
                            }
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
            foreach (var pathParameter in pathItem.Value.Parameters)
            {
                if (pathParameter.In == ParameterLocation.Path && pathParameterNameToReplace.Equals(pathParameter.Name, StringComparison.Ordinal))
                {
                    pathParameter.Name = pathParameterNameReplacement;
                }
            }

            foreach (var operation in pathItem.Value.Operations.Values)
            {
                foreach (var operationParameter in operation.Parameters)
                {
                    if (operationParameter.In == ParameterLocation.Path && pathParameterNameToReplace.Equals(operationParameter.Name, StringComparison.Ordinal))
                    {
                        operationParameter.Name = pathParameterNameReplacement;
                    }
                }
            }

            if (source != destination && !destination.PathItems.TryAdd(pathItem.Key, pathItem.Value))
            {
                var destinationPathItem = destination.PathItems[pathItem.Key];
                foreach (var operation in pathItem.Value.Operations)
                    if (!destinationPathItem.Operations.TryAdd(operation.Key, operation.Value))
                    {
                        logger.LogWarning("Duplicate operation {Operation} in path {Path}", operation.Key, pathItem.Key);
                    }
                foreach (var pathParameter in pathItem.Value.Parameters)
                    destinationPathItem.Parameters.Add(pathParameter);
                foreach (var extension in pathItem.Value.Extensions)
                    if (!destinationPathItem.Extensions.TryAdd(extension.Key, extension.Value))
                    {
                        logger.LogWarning("Duplicate extension {Extension} in path {Path}", extension.Key, pathItem.Key);
                    }
            }
        }
    }
}
