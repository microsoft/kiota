using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.Configuration;
using Microsoft.OpenApi;

namespace Kiota.Builder.Extensions;

public static class OpenApiOperationExtensions
{
    internal static readonly HashSet<string> SuccessCodes = new(StringComparer.OrdinalIgnoreCase) { "200", "201", "202", "203", "206", "2XX" }; //204 excluded as it won't have a schema
    private static string vendorSpecificCleanup(string input)
    {
        var slashIndex = input.IndexOf('/', StringComparison.OrdinalIgnoreCase);
        var plusIndex = input.IndexOf('+', StringComparison.OrdinalIgnoreCase);
        if (slashIndex == -1 || plusIndex == -1)
            return input;
        if (plusIndex < slashIndex)
            return input;
        return input[0..(slashIndex + 1)] + input[(plusIndex + 1)..];
    }
    /// <summary>
    /// cleans application/vnd.github.mercy-preview+json to application/json
    /// </summary>
    internal static IOpenApiSchema? GetResponseSchema(this OpenApiOperation operation, StructuredMimeTypesCollection structuredMimeTypes)
    {
        ArgumentNullException.ThrowIfNull(operation);
        // Return Schema that represents all the possible success responses!
        return operation.GetResponseSchemas(SuccessCodes, structuredMimeTypes)
                            .FirstOrDefault();
    }
    internal static IEnumerable<IOpenApiSchema> GetResponseSchemas(this OpenApiOperation operation, HashSet<string> successCodesToUse, StructuredMimeTypesCollection structuredMimeTypes)
    {
        if (operation.Responses is null) return [];
        // Return Schema that represents all the possible success responses!
        return operation.Responses.Where(r => successCodesToUse.Contains(r.Key))
                            .OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase)
                            .SelectMany(re => re.Value.Content?.GetValidSchemas(structuredMimeTypes) ?? []);
    }
    internal static IOpenApiSchema? GetRequestSchema(this OpenApiOperation operation, StructuredMimeTypesCollection structuredMimeTypes)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return operation.RequestBody?.Content
                            ?.GetValidSchemas(structuredMimeTypes).FirstOrDefault();
    }
    private static readonly StructuredMimeTypesCollection multipartMimeTypes = new(["multipart/form-data"]);
    internal static bool IsMultipartFormDataSchema(this IDictionary<string, IOpenApiMediaType> source, StructuredMimeTypesCollection structuredMimeTypes)
    {
        return source.GetValidSchemas(structuredMimeTypes).FirstOrDefault() is IOpenApiSchema schema &&
        source.GetValidSchemas(multipartMimeTypes).FirstOrDefault() == schema;
    }
    internal static bool IsMultipartTopMimeType(this IDictionary<string, IOpenApiMediaType> source, StructuredMimeTypesCollection structuredMimeTypes)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(structuredMimeTypes);
        if (structuredMimeTypes.Count == 0) return false;
        if (!source.ContainsKey(multipartMimeTypes.First())) return false;
        if (source.Count == 1 || !source.Keys.Where(static x => !multipartMimeTypes.Contains(x)).Any(structuredMimeTypes.Contains)) return true;
        return structuredMimeTypes.First() == multipartMimeTypes.First();
    }
    internal static IEnumerable<IOpenApiSchema> GetValidSchemas(this IDictionary<string, IOpenApiMediaType> source, StructuredMimeTypesCollection structuredMimeTypes)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(structuredMimeTypes);
        if (structuredMimeTypes.Count == 0)
            throw new ArgumentNullException(nameof(structuredMimeTypes));
        return source
                    .Where(static c => !string.IsNullOrEmpty(c.Key))
                    .Select(static c => (Key: c.Key.Split(';', StringSplitOptions.RemoveEmptyEntries)[0], c.Value))
                    .Where(c => structuredMimeTypes.Contains(c.Key) || structuredMimeTypes.Contains(vendorSpecificCleanup(c.Key)))
                    .Select(static co => co.Value.Schema)
                    .OfType<IOpenApiSchema>();
    }
    internal static IOpenApiSchema? GetResponseSchema(this IOpenApiResponse response, StructuredMimeTypesCollection structuredMimeTypes)
    {
        ArgumentNullException.ThrowIfNull(response);
        return response.Content?.GetValidSchemas(structuredMimeTypes).FirstOrDefault();
    }
}
