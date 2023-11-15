using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Kiota.Builder.Configuration;
using Microsoft.OpenApi.Models;

namespace Kiota.Builder.Extensions;
public static class OpenApiOperationExtensions
{
    internal static readonly HashSet<string> SuccessCodes = new(StringComparer.OrdinalIgnoreCase) { "200", "201", "202", "203", "206", "2XX" }; //204 excluded as it won't have a schema
    /// <summary>
    /// cleans application/vnd.github.mercy-preview+json to application/json
    /// </summary>
    private static readonly Regex vendorSpecificCleanup = new(@"[^/]+\+", RegexOptions.Compiled, Constants.DefaultRegexTimeout);
    internal static OpenApiSchema? GetResponseSchema(this OpenApiOperation operation, StructuredMimeTypesCollection structuredMimeTypes)
    {
        ArgumentNullException.ThrowIfNull(operation);
        // Return Schema that represents all the possible success responses!
        return operation.GetResponseSchemas(SuccessCodes, structuredMimeTypes)
                            .FirstOrDefault();
    }
    internal static IEnumerable<OpenApiSchema> GetResponseSchemas(this OpenApiOperation operation, HashSet<string> successCodesToUse, StructuredMimeTypesCollection structuredMimeTypes)
    {
        // Return Schema that represents all the possible success responses!
        return operation.Responses.Where(r => successCodesToUse.Contains(r.Key))
                            .OrderBy(static x => x.Key, StringComparer.OrdinalIgnoreCase)
                            .SelectMany(re => re.Value.Content.GetValidSchemas(structuredMimeTypes));
    }
    internal static OpenApiSchema? GetRequestSchema(this OpenApiOperation operation, StructuredMimeTypesCollection structuredMimeTypes)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return operation.RequestBody?.Content
                            .GetValidSchemas(structuredMimeTypes).FirstOrDefault();
    }
    private static readonly StructuredMimeTypesCollection multipartMimeTypes = new(new string[] { "multipart/form-data" });
    internal static bool IsMultipartFormDataSchema(this IDictionary<string, OpenApiMediaType> source, StructuredMimeTypesCollection structuredMimeTypes)
    {
        return source.GetValidSchemas(structuredMimeTypes).FirstOrDefault() is OpenApiSchema schema &&
        source.GetValidSchemas(multipartMimeTypes).FirstOrDefault() == schema;
    }
    internal static IEnumerable<OpenApiSchema> GetValidSchemas(this IDictionary<string, OpenApiMediaType> source, StructuredMimeTypesCollection structuredMimeTypes)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(structuredMimeTypes);
        if (structuredMimeTypes.Count == 0)
            throw new ArgumentNullException(nameof(structuredMimeTypes));
        return source
                    .Where(static c => !string.IsNullOrEmpty(c.Key))
                    .Select(static c => (Key: c.Key.Split(';', StringSplitOptions.RemoveEmptyEntries)[0], c.Value))
                    .Where(c => structuredMimeTypes.Contains(c.Key) || structuredMimeTypes.Contains(vendorSpecificCleanup.Replace(c.Key, string.Empty)))
                    .Select(static co => co.Value.Schema)
                    .Where(static s => s is not null);
    }
    internal static OpenApiSchema? GetResponseSchema(this OpenApiResponse response, StructuredMimeTypesCollection structuredMimeTypes)
    {
        ArgumentNullException.ThrowIfNull(response);
        return response.Content.GetValidSchemas(structuredMimeTypes).FirstOrDefault();
    }
}
