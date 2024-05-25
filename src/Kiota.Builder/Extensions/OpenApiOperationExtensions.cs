using System;
using System.Collections.Generic;
using Kiota.Builder.Configuration;
using Microsoft.OpenApi.Models;

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
    internal static OpenApiSchema? GetResponseSchema(this OpenApiOperation operation, StructuredMimeTypesCollection structuredMimeTypes)
    {
        ArgumentNullException.ThrowIfNull(operation);
        // Return Schema that represents all the possible success responses!
        var responseSchemas = operation.GetResponseSchemas(SuccessCodes, structuredMimeTypes);
        OpenApiSchema? firstSchema = null;
        foreach (var schema in responseSchemas)
        {
            firstSchema = schema;
            break;
        }
        return firstSchema;
    }
    internal static IEnumerable<OpenApiSchema> GetResponseSchemas(this OpenApiOperation operation, HashSet<string> successCodesToUse, StructuredMimeTypesCollection structuredMimeTypes)
    {
        // Prepare a list to hold the schemas
        List<OpenApiSchema> schemas = new List<OpenApiSchema>();

        // Prepare a sorted list to hold the responses
        SortedList<string, OpenApiResponse> sortedResponses = new SortedList<string, OpenApiResponse>(StringComparer.OrdinalIgnoreCase);

        // Filter the responses and add them to the sorted list
        foreach (var response in operation.Responses)
        {
            if (successCodesToUse.Contains(response.Key))
            {
                sortedResponses.Add(response.Key, response.Value);
            }
        }

        // Get the schemas from the sorted responses and add them to the list
        foreach (var response in sortedResponses)
        {
            schemas.AddRange(response.Value.Content.GetValidSchemas(structuredMimeTypes));
        }

        return schemas;
    }
    internal static OpenApiSchema? GetRequestSchema(this OpenApiOperation operation, StructuredMimeTypesCollection structuredMimeTypes)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var validSchemas = operation.RequestBody?.Content.GetValidSchemas(structuredMimeTypes);
        OpenApiSchema? firstSchema = null;
        if (validSchemas != null)
        {
            foreach (var schema in validSchemas)
            {
                firstSchema = schema;
                break;
            }
        }
        return firstSchema;
    }
    private static readonly StructuredMimeTypesCollection multipartMimeTypes = new(["multipart/form-data"]);
    internal static bool IsMultipartFormDataSchema(this IDictionary<string, OpenApiMediaType> source, StructuredMimeTypesCollection structuredMimeTypes)
    {
        using var enumerator1 = source.GetValidSchemas(structuredMimeTypes).GetEnumerator();
        using var enumerator2 = source.GetValidSchemas(multipartMimeTypes).GetEnumerator();
        return enumerator1.MoveNext() && enumerator2.MoveNext() && enumerator1.Current == enumerator2.Current;
    }
    internal static IEnumerable<OpenApiSchema> GetValidSchemas(this IDictionary<string, OpenApiMediaType> source, StructuredMimeTypesCollection structuredMimeTypes)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(structuredMimeTypes);
        if (structuredMimeTypes.Count == 0)
            throw new ArgumentNullException(nameof(structuredMimeTypes));

        List<OpenApiSchema> validSchemas = new List<OpenApiSchema>();

        foreach (var item in source)
        {
            if (!string.IsNullOrEmpty(item.Key))
            {
                var keyParts = item.Key.Split(';', StringSplitOptions.RemoveEmptyEntries);
                if (keyParts.Length > 0)
                {
                    var key = keyParts[0];
                    if (structuredMimeTypes.Contains(key) || structuredMimeTypes.Contains(vendorSpecificCleanup(key)))
                    {
                        if (item.Value.Schema is not null)
                        {
                            validSchemas.Add(item.Value.Schema);
                        }
                    }
                }
            }
        }

        return validSchemas;
    }
    internal static OpenApiSchema? GetResponseSchema(this OpenApiResponse response, StructuredMimeTypesCollection structuredMimeTypes)
    {
        ArgumentNullException.ThrowIfNull(response);
        using var enumerator = response.Content.GetValidSchemas(structuredMimeTypes).GetEnumerator();
        return enumerator.MoveNext() ? enumerator.Current : null;
    }
}
